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
                <!-- <template> -->
                    <v-col class="container" cols="12">
                        <v-card elevation="12" class="cards" v-for="item in movieList" :key="item.imdbID">
                        <!-- TODO:: display title text on top of placeholder image -->
                        <div @click="hoverImg(item.imdbID)">
                            <v-img class="title-poster" :src="`${item.poster}`">  </v-img>
                        </div>
                        
                        <!-- <v-img class="image-height" :src="item.poster == 'N/A' ? `@/assets/img/title-placeholder.png` : `${item.poster}`"> </v-img> -->
                        <!-- <v-card-text>
                            <v-row class="card-text mt-2 "> <strong>Name : </strong> {{item.title}}</v-row>
                            <v-row class="card-text mt-2 "> </v-row>
                            <v-row class="card-text mt-6 "> <strong>Year : </strong> {{item.year}}</v-row>                            
                        </v-card-text> -->
                        </v-card>
                    </v-col>
                <!-- </template> -->
            </v-row>             
            <!-- <div class="row">
                <div class="col-lg-12">
                    <h1 class="page-header">Top Movies</h1>
                </div>
            </div>

            <!-- <div class="row"> -->
                <!-- <div class="col-lg-3 col-md-6">
                    <div class="panel panel-primary height-sm">

                    </div>
                </div> -->
                <!-- <div class="col-lg-4 col-md-6">
                    <div class="panel panel-primary height-sm">

                    </div>
                </div>
                <div class="col-lg-5 col-md-6">
                    <div class="panel panel-primary height-sm">

                    </div>
                </div> -->
            <!-- </div> -->

            <!-- <div class="row"  id="dashboardLastRow">
                <div class="col-sm-12 col-lg-6 flexColumn">
                    <div class="panel panel-primary flex3">
                        <div class="panel-heading">Footer</div>
                        <div class="panel-body">
                        </div>
                    </div>
                </div> 
            </div> -->
        </div> <!-- /content-wrapper -->
    </div> <!-- /#page-wrapper -->
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
            // this.$vuetify.theme.dark = true;
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
</style>
