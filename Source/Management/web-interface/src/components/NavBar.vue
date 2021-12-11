<template>
    <nav class="navbar navbar-default  navbar-static-top" role="navigation">

    <!-- <costom-modal-dialog v-if="showNavDialog" @no="showNavDialog = false" @yes="unsevedChangesYes">
      <div slot="header">Unsaved Changes</div>
      <div slot="body">{{unsavedNavDialogMessage}}<br/>Do you want to discard them and continue?</div>
    </costom-modal-dialog> -->

      <div class="navbar-header">
        <button type="button" class="navbar-toggle" data-toggle="collapse" data-target=".navbar-collapse">
                      <span class="sr-only">Toggle navigation</span>
                      <span class="icon-bar"></span>
                      <span class="icon-bar"></span>
                      <span class="icon-bar"></span>
                  </button>
        <a class="navbar-brand" href="#"> </a>
      </div>
  
      <li>
        <div class="search-wrapper">
          <input class="search-key" type="text" @keyup="searchMovies" v-model="searchQuery" placeholder="Search Title..."/>
          <!-- <div class="pull-right">
              <button type="submit" class="btn-search-movie" @click="searchMovies">{{waitingForSearchResults ? "Searching..." : "Search"}}</button>
          </div> -->
        </div>

        
      </li>
    </nav>
</template>

<script>
  import Vue from 'vue'

  import { eventBus } from '../main'
  import { mapGetters } from 'vuex';

  var proto_messages = require("../proto/Messages_pb.js");

  export default {
    data() {
      return {
        searchQuery: "",
        waitingForSearchResults: false
        // systemStatus: proto_messages.GeneralStatus.GOOD,
        // connectedToAgent: false,
        // showAdvancedNavBar: this.$store.getters.getAdvancedMode,
        // mainNavBar: [
        //   { title: 'Dashboard',     icon: 'icon icon-dashboard',    name: 'status-page',        route: '',              visible: true }
        // ],
        // showNavDialog: false,
        // pageTransitionPanding: ""
      }
    },
    methods: {
      navigateTo(route) {
          // mixpanel.track("Navigate to " + route + " from " + this.$route.name);
          this.$router.push("/" + route);
          this.pageTransitionPanding = "";
      },
      // goToPage(route) {
      //   if (this.$store.getters.isDirty)
      //   {
      //     this.showNavDialog = true;
      //     this.pageTransitionPanding = route;
      //   }
      //   else
      //   {
      //     this.navigateTo(route);
      //   }
      // },
      unsevedChangesYes() {
        this.showNavDialog = false
        this.$store.commit('setIsDirty', false);
        this.navigateTo(this.pageTransitionPanding);
      },
      searchMovies() {
          console.log("sending search movies request")
          this.waitingForSearchResults = true;
          var newMsg = new proto_messages.TitlesSearch();
          newMsg.setSearchquery(this.searchQuery);
          this.$store.dispatch('sendMessageAsWrappedMessage', {
              msg: newMsg,
              msgType: "ProtoMessages.TitlesSearch"
          });
      }
    },
    computed: {
      ...mapGetters({
        
      }),
      currentRoute() {
        return this.$route.name;
      },
      isDevMode() {
        return Vue.config.devtools;
      }
    },
    created () {
      eventBus.$on('TitlesSearchReply', payload => {
        console.log("recived TitlesSearchReply");
        this.waitingForSearchResults = false;
      });
    }
  }
</script>