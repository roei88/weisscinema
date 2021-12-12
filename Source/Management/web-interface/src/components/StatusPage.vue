<template>
    <div id="page-wrapper">
        <hover-frame v-if="showHoverFrame && !watingForTitle" @Close="unhoverImg()" @Add="addToWishlist()" @Remove="removeFromWishlist()"></hover-frame>
        <div class="contentWrapper">
              <v-row>
                    <v-col class="container" cols="12">
                        <v-card elevation="12" class="cards" v-for="item in movieList" :key="item.imdbID">
                            <div class="heart-icon svg">
                                <svg data-v-1e613282="" version="1.1" viewBox="0 0 18 18" class="svg-icon" style="hover: "><path stroke="rgb(var(--color-red))"  pid="0" d="M15.63 3.458a4.125 4.125 0 0 0-5.835 0L9 4.253l-.795-.795A4.126 4.126 0 1 0 2.37 9.293l.795.795L9 15.922l5.835-5.835.795-.795a4.125 4.125 0 0 0 0-5.835v0z" _stroke="#EF5C5C" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"></path></svg>
                            </div> 
                            <div class="card-container">
                                <v-img class="title-poster " :src="`${item.poster}`" :id="`${item.imdbID}`" alt> </v-img>
                                <div class="overlay" @click="hoverImg(item.imdbID)">        
                                    <div class="text">{{ item.title }} <br><br> {{ item.year }}</div>
                                </div>
                            </div>
                        </v-card>
                    </v-col>
            </v-row> 
            </div> 
        </div> 
</template>

<script>
    import { eventBus } from '../main';
    import { mapGetters } from 'vuex';
    var moment = require('moment');
    var proto_messages = require("../proto/Messages_pb.js");

    export default {
        data() {
            return {
                showHoverFrame: false,
                watingForTitle: false,
                currentImdbID: "",
                popupTitle: new proto_messages.MovieTitle()
            }
        },
        computed: {
            ...mapGetters({
                movieList : "movieList",
                titleList: "titleList",
            })
        },
        methods: {
            hoverImg(imdbID) {
                console.log("hovering on: "+imdbID)
                this.currentImdbID = imdbID;
                
                if (!this.titleList.has(imdbID))
                {
                    this.watingForTitle = true;
                    var newMsg = new proto_messages.MovieTitle();
                    newMsg.setImdbid(imdbID);
                    this.$store.dispatch('sendMessageAsWrappedMessage', {
                        msg: newMsg,
                        msgType: "ProtoMessages.MovieTitle"
                    });
                }
                else
                {
                    this.udpateCurrentPopupTitle();
                }

                this.showHoverFrame = true;
            },
            unhoverImg() {
                this.showHoverFrame = false;
                this.currentImdbID = "";
            },
            udpateCurrentPopupTitle()
            {
                this.$store.commit('setPopupTitle', this.titleList.get(this.currentImdbID));
                this.watingForTitle = false;
            },
            addToWishlist() {
                this.$store.dispatch("addToWishlist", {
                    value: this.currentImdbID
                })
            },
            removeFromWishlist()
            {
                this.$store.dispatch("removeFromWishlist", {
                    value: this.currentImdbID
                })
            }
        },
        created() {
            eventBus.$on('MovieTitleReply', payload => {
                console.log("recived MovieTitleReply");
                console.log("getting popup for: "+this.currentImdbID);
                console.log(this.popupTitle);
                this.udpateCurrentPopupTitle();
            });
        },
        watch: {
            initialDataReceived: {
                immediate: true,
                handler(value) {
                    if (value) {
                        console.log("value");
                    }
                }
            }
        }
    }
</script>
