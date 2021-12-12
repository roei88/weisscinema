<template>
    <nav class="navbar navbar-default  navbar-static-top" role="navigation">
      <div class="navbar-header">
        <a class="navbar-brand" href="#"> </a>
      </div>
      <li>
        <div class="search-wrapper">
          <v-row>
            <v-col>
              <v-autocomplete class="search-key" :items="entries" :search-input.sync="searchQuery" background-color="white" hide-details hide-no-data dense filled label="Search Title..." @keyup="searchMovies"></v-autocomplete> 
            </v-col>           
            
            <v-col>
              <button type="submit" class="btn-search-movie" @click="showWishlist()">{{wishliston ? "Hide Wishlist" : "Show Wishlist" }}</button>
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
        queryTerm: '',
        wishliston: false
      }
    },
    methods: {
      searchMovies() {
        if (!this.wishliston)
        {
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
      showWishlist()
      {
          this.wishliston = !this.wishliston;
          this.$store.dispatch('showwishlist', {
              on: this.wishliston
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
