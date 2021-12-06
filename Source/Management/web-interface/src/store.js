import Vue from 'vue';
import Vuex from 'vuex';
import { eventBus} from './main';
var Promise = require('es6-promise').Promise;
require('es6-promise').polyfill();
Vue.use(Vuex);
let appVersion = require('./version');
var proto_messages = require("./proto/Messages_pb.js");
var wsR = require('./assets/js/reconnecting-websocket.min.js');
var token = getQueryStringValue("token");

function getQueryStringValue (key) {  
    return decodeURIComponent(window.location.search.replace(new RegExp("^(?:.*[&\\?]" + encodeURIComponent(key).replace(/[\.\+\*]/g, "\\$&") + "(?:\\=([^&]*))?)?.*$", "i"), "$1"));  
}

function getIP()
{
    var coordinatorIP = getQueryStringValue('ws');
    if (coordinatorIP == "")
    {
        if (location.host.indexOf(":") != -1)
        {
            coordinatorIP = location.host.split(':')[0];
        }
        else
        {
            coordinatorIP = location.host;    
        }
    }
    return coordinatorIP;
}

function getPort()
{
    var coordinatorPort = getQueryStringValue('port');
    if (coordinatorPort == "")
    {
        coordinatorPort = 9001;
    }
    return coordinatorPort;
}

function getUrlScheme()
{
    return (location.protocol == 'https:') ? "wss" : "ws";
}


function combineUrl()
{
    var urlScheme = getUrlScheme();     // ws or wss
    var address = getIP();
    var port = getPort();
    var tokenPart = "";
    if (token != "")
    {
        tokenPart = "?token=" + token;
    }
    var url = urlScheme + '://' + address + ':' + port + tokenPart;
    return url;
}

var watchDogIsConnected = false
var watchDogInterval = setInterval(function() {
    watchDogIsConnected = false;
}, 5000);

var socketUrl = combineUrl();
var socket = new wsR(socketUrl);
socket.onopen = function () {
    store.dispatch('wsConnectionChanged', true);
    setTimeout(function() {
        store.dispatch("sendUIVersion");    
    }, 1000);
};

socket.onmessage = function (evt) {
    watchDogIsConnected = true;
    var receivedMsg = evt.data;
    var reader = new FileReader();
    reader.addEventListener("loadend", function () {
        try
        {
            store.dispatch('wsMessageArrived', reader.result);
        }
        catch(e)        
        {
            console.error("Caught exception: " + e.toString());
        }
    });
    reader.readAsArrayBuffer(receivedMsg);
};

export const store = new Vuex.Store({
    state: {
        wsSocket: socket,
        connectedToCoordinator: false,
        // articles: new proto_messages.ArticlesMessage(),
    },

    actions: {
        wsConnectionChanged(context, isConnected) {
            if (isConnected == false)
            {
                context.commit('setInitialDataReceived', false);
            }
        },

        wsMessageArrived(context, message) {
            try
            {
                var coordinatorMsg = proto_messages.WrappedMessage.deserializeBinary(message);
                var msgTypeStr = coordinatorMsg.getMessagetypestr();
                var msgContent = coordinatorMsg.getMessagecontent();
                switch (msgTypeStr)
                {
                    case "ProtoMessages.ArticlesMessage":
                        context.dispatch('handleIncomingArticlesMessage', msgContent);
                        break;
                }
            }
            catch (e)
            {
                console.error("Failed to deserialize a WrappedMessage, error: " + e.toString());
            }
        },
        
        handleIncomingArticlesMessage(context, msgContent) 
        {
            try 
            {
                context.state.articles = proto_messages.ArticlesMessage.deserializeBinary(msgContent);
                eventBus.$emit('ArticlesReply', { });

            }
            catch (e) {
                console.error("Failed to handle incoming articles message, error: " + e.toString());
            }
        },

        sendUIVersion(context) 
        {
            console.log("sendUIVersion begin with"+appVersion.version);
            var message = new proto_messages.VersionMessage();
            message.setVersion(appVersion.version);
            console.log("Sending version: " + appVersion.version);
            context.dispatch('sendMessageAsWrappedMessage', {
                msg: message,
                msgType: "ProtoMessages.VersionMessage"
            });
        },

        sendRawData(context, protoMsg) {
            if (context.state.wsSocket.readyState === 1)
            {
                var buf = protoMsg.serializeBinary();
                context.state.wsSocket.send(buf.buffer);
            }
            else
            {
                console.warn("Cannot send data, socket.readyState is " + context.state.wsSocket.readyState);
            }
        },

        sendMessageAsWrappedMessage(context, payload) {
            var wrappedMessage = new proto_messages.WrappedMessage();
            wrappedMessage.setMessagetypestr(payload.msgType);
            wrappedMessage.setMessagecontent(payload.msg.serializeBinary());
            context.dispatch('sendRawData', wrappedMessage);
        }
    },

    getters: {
        getArticles(state) {
            var ret = undefined;
            if (state.articles)
            {
                ret = state.articles;
            }
            return ret;                          
        }
    }
});
