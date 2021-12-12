<template>
    <div id="page-wrapper">

        <hover-frame v-if="showHoverFrame && !watingForTitle" @Close="unhoverImg()">
            <!-- <div slot="header">{{popupTitle.getTitle()}}</div>
            <div slot="body" class="col-xs-12">
                Body
            </div> -->
        </hover-frame>


        <div class="contentWrapper">
            
              <v-row>
                    <v-col class="container" cols="12">
                        <v-card elevation="12" class="cards" v-for="item in movieList" :key="item.imdbID">
                        <div class="card-container" @click="hoverImg(item.imdbID)">
                            <v-img class="title-poster " :src="`${item.poster}`" alt> </v-img>
                            <div class="overlay">        
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
            navigateTo(route) {
                this.$router.push("/" + route);
            },
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

//TODO:: move to costom css file 

<style>
.title-poster {
  min-width: 200px;
  max-width: 200px;
  min-height: 300px;
  max-height: 300px;
}

.container {
  display: flex;
  flex-wrap: wrap;
  justify-content: center;
}

.cards {
  height: 30%;
  width: 20%;
  margin: 1%;
}

.card-text {
  text-transform: capitalize;
  font-size: 15px;
  margin-block: 1px;
}

.container::after {
  display: flex;
  flex-wrap: wrap;
}

.row {
  display: block !important;
}

.overlay {
  position: absolute;
  top: 0;
  bottom: 0;
  left: 0;
  right: 0;
  height: 100%;
  width: 100%;
  opacity: 0;
  transition: .5s ease;
}

.card-container:hover .overlay {
  opacity: 100%;
}

.card-container:hover .title-poster {
  opacity: 40%;
}

.text {
  color: rgb(255, 174, 0);
  font-size: 20px;
  font-weight: bold;
  position: absolute;
  top: 50%;
  left: 50%;
  -webkit-transform: translate(-50%, -50%);
  -ms-transform: translate(-50%, -50%);
  transform: translate(-50%, -50%);
  text-align: center;
}

</style>
