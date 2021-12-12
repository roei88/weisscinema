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
          <v-row>
            <v-col>
              <v-autocomplete class="search-key" :items="entries" :search-input.sync="searchQuery" background-color="white"   hide-details hide-no-data dense filled label="Search Title..." @keyup="searchMovies"></v-autocomplete>
            </v-col>
          </v-row>
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
        waitingForSearchResults: false,
        newTag: '',
        queryTerm: ''
      }
    },
    methods: {
      navigateTo(route) {
          this.$router.push("/" + route);
          this.pageTransitionPanding = "";
      },
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
        entries: "entries"
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
<style>
.v-autocomplete {
  max-height: 30px;
}
</style>
