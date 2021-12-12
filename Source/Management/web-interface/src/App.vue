

<template>
<v-app>

    <div id="app">
        <costom-nav-bar></costom-nav-bar>
        <router-view></router-view>
    </div>
    </v-app>
</template>


<script>
    import { eventBus} from './main'
    import { mapGetters } from 'vuex';   
    
    let appVersion = require('./version');

    var proto_messages = require("./proto/Messages_pb.js");

    export default {
        name: 'app',
        data() {
            return { 
                switchTimeout: 0,
                showForceLeaveSetupMode: false,
                showSetupModeFailure: false
            }
        }, 
        methods: {
            
        },
        computed: {
            ...mapGetters({
                initialDataReceived: "initialDataReceived"
            }),            
        },
        created () {
            eventBus.$on('newMsg', () => {
                console.log("newMsg");
            });
        }
    }
</script>

