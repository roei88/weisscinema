<template>  
  <transition name="modal">
    <div class="modal-mask">
      <div class="modal-dialog">
        <div class="modal-content">
                <div class="card flex">
                    <div class="card-image">
                        <img :src="`${popupTitle.getPoster()}`">
                    </div>
                    <div class="card-content">
                        <div class="head-text">
                            <h4> {{popupTitle.getTitle()}}</h4>
                            {{popupTitle.getYear()}}
                        </div>

                        <div class="description body-text">
                            <p> {{popupTitle.getPlot()}} </p>
                            <p> <strong> IMDB Ratings: </strong> {{popupTitle.getImdbrating()}} </p>
                            <p> <strong> Actors: </strong> {{popupTitle.getActors()}} </p>
                            <p> <strong> Genres: </strong> {{popupTitle.getGenre()}} </p>
                            <p> <strong> Actors: </strong> {{popupTitle.getActors()}} </p>
                            <p> <strong> Writers: </strong> {{popupTitle.getWriter()}} </p>
                            <p> <strong> Rlease Date: </strong> {{popupTitle.getReleased()}} </p>
                            <p> <strong> Language: </strong> {{popupTitle.getLanguage()}} </p>
                            <p> <strong> Country: </strong> {{popupTitle.getCountry()}} </p>

                        </div>
                        <div class="cta">
                            <button>
                                Add To Wishlist
                            </button>
                        </div>
                        <button class="btn-search-movie" @click="$emit('Close')"> {{ closeButton }}</button>
                    </div>
                </div>
        </div>
      </div>
    </div>
  </transition>
</template>

<script>
    import { eventBus } from '../main';
    import { mapGetters } from 'vuex';
    
    var moment = require('moment');
    var proto_messages = require("../proto/Messages_pb.js");

    export default {
        props: {
            closeButton: {
                type: String,
                default: "Close",
            }
        },
        data() {
            return {

            }
        },
        computed: {
            ...mapGetters({
                popupTitle : "popupTitle",
            })
        },
        methods: {

        },
        created() {

        }
    }
</script>  
