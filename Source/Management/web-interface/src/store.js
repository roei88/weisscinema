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
    var ip = getQueryStringValue('ws');
    if (ip == "")
    {
        if (location.host.indexOf(":") != -1)
        {
            ip = location.host.split(':')[0];
        }
        else
        {
            ip = location.host;    
        }
    }
    return ip;
}

function getPort()
{
    var port = getQueryStringValue('port');
    if (port == "")
    {
        port = 9001;
    }
    return port;
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
        connected: false,
        titles: new proto_messages.TitlesSearch(),
        newTitle: new proto_messages.MovieTitle(),
        popupTitle: new proto_messages.MovieTitle(),
        movieList: [],
        tempMovieList: [],
        titleList: new Map(),
        entries: [],
        showingWishlist: false,
        placeholder: "data:image/png;base64,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"
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
                var msg = proto_messages.WrappedMessage.deserializeBinary(message);
                var msgTypeStr = msg.getMessagetypestr();
                var msgContent = msg.getMessagecontent();
                console.log("incoming msg: " + msgTypeStr);
                switch (msgTypeStr)
                {
                    case "ProtoMessages.TitlesSearch":
                        context.dispatch('handleIncomingTitlesSearchMessage', msgContent);
                        break;
                    case "ProtoMessages.MovieTitle":
                        context.dispatch('handleIncomingMovieTitleMessage', msgContent);
                        break;
                }
            }
            catch (e)
            {
                console.error("Failed to deserialize a WrappedMessage, error: " + e.toString());
            }
        },
        
        // TODO:: replace map keys check on re-init/check on server side
        handleIncomingTitlesSearchMessage(context, msgContent) 
        {
            try 
            {
                if (context.state.showingWishlist)
                {
                    return;   
                }

                context.state.titles = proto_messages.TitlesSearch.deserializeBinary(msgContent);
                var protoMoviesList = context.state.titles.getSearchList(); 
                var tempMovieList = [];
                var newEntries = [];
                var keys = new Map();
                for(var i = 0; i < protoMoviesList.length; i++) 
                {
                    var singleTitle = {imdbID: "", poster: "", title: "", year: ""};
                    if (protoMoviesList[i].getPoster()=="N/A")
                    {
                        singleTitle.poster = context.state.placeholder;
                    }
                    else
                    {
                        singleTitle.poster = protoMoviesList[i].getPoster();
                    }
                    singleTitle.title = protoMoviesList[i].getTitle();
                    singleTitle.year = protoMoviesList[i].getYear();
                    singleTitle.imdbID = protoMoviesList[i].getImdbid();
                    if (!keys.has(singleTitle.imdbID))
                    {
                        keys.set(singleTitle.imdbID, singleTitle.title);
                        tempMovieList.push(singleTitle);
                    }
                    newEntries.push(protoMoviesList[i].getTitle());
                }
                context.state.entries = newEntries;
                context.state.movieList = tempMovieList;
                console.log(context.state.movieList);
                eventBus.$emit('TitlesSearchReply', { });
            }
            catch (e) {
                console.error("Failed to handle incoming TitlesSearch message, error: " + e.toString());
            }
        },

        handleIncomingMovieTitleMessage(context, msgContent) 
        {
            try 
            {

                context.state.titles.newTitle = proto_messages.MovieTitle.deserializeBinary(msgContent);                
                var imdbID = context.state.titles.newTitle.getImdbid();

                if (!context.state.titleList.has(imdbID))
                {
                    context.state.titleList.set(imdbID, context.state.titles.newTitle);
                    eventBus.$emit('MovieTitleReply', { });
                }
                
            }
            catch (e) {
                console.error("Failed to handle incoming MovieTitle message, error: " + e.toString());
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
        },
        addToWishlist(context, payload) {
            console.log("adding "+ payload.value);
            context.state.titles.setAddwishtitle(payload.value);;

            context.dispatch('sendMessageAsWrappedMessage', {
                msg: context.state.titles,
                msgType: "ProtoMessages.TitlesSearch"
            });
        },
        removeFromWishlist(context, payload) {
            context.state.titles.setRemovewishtitle(payload.value);;

            context.dispatch('sendMessageAsWrappedMessage', {
                msg: context.state.titles,
                msgType: "ProtoMessages.TitlesSearch"
            });
        },
        
        showwishlist(context, payload) 
        {
            try 
            {
                console.log("got show playlist with: "+payload.on);
                if (payload.on)
                {
                    context.state.tempMovieList = context.state.movieList;
                    var wishlist = context.state.titles.getWishtitlesMap();
                    console.log(wishlist);
                    var tempMovieList = [];
                    if (wishlist != null)
                    {
                        for (let value of wishlist.values()){
                            console.log(value.getTitle());
                            var singleTitle = {imdbID: "", poster: "", title: "", year: ""};
                            if (value.getPoster()=="N/A")
                            {
                                singleTitle.poster = context.state.placeholder;
                            }
                            else
                            {
                                singleTitle.poster = value.getPoster();
                            }
                            singleTitle.title = value.getTitle();
                            singleTitle.year = value.getYear();
                            singleTitle.imdbID = value.getImdbid();
                            tempMovieList.push(singleTitle);
                        }
                    }

                    context.state.movieList = tempMovieList;
                }
                else
                {
                    context.state.movieList = context.state.tempMovieList;
                }

                eventBus.$emit('TitlesSearchReply', { });
            }
            catch (e) {
                console.error("Failed to handle show/hide wish list, error: " + e.toString());
            }
        }

    },

    mutations: {
        setPopupTitle(state, value) {
            state.popupTitle = value;
        }
    },

    getters: {
        movieList(state) {
            return state.movieList;
        },
        titleList(state) {
            return state.titleList;
        },
        popupTitle(state) {
            return state.popupTitle;
        },
        entries(state) {
            return state.entries;
        },
        wishList(state) {
            return state.titles.getWishtitlesMap();
        },
        inWishList(state) {
            console.log(state.titles.getWishtitlesMap().has(state.popupTitle.getImdbid()));
            return state.titles.getWishtitlesMap().has(state.popupTitle.getImdbid());
        }
    }
});
