<template>
    <div id="page-wrapper">
        <div class="contentWrapper">

              <v-row>
                <!-- <v-template> -->
                <v-col class="container" cols="12">
                    <v-card elevation="12" class="cards" v-for="item in movieList" :key="item.imdbID">
                    <v-img class="image-height" :src="`${item.Poster}`"> </v-img>
                    <v-card-text>
                        <v-row class="card-text mt-2 "> <strong>Name : </strong> {{item.title}}</v-row>
                        <v-row class="card-text mt-6 "> <strong>Type : </strong> {{item.Type}}</v-row>
                        <v-row class="card-text mt-6 "> <strong>Year : </strong> {{item.Year}}</v-row>
                    </v-card-text>
                    </v-card>
                </v-col>
                <!-- </v-template> -->
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

            }
        },
        computed: {
            ...mapGetters({
                movieList : "movieList"
            })
        },
        methods: {
            navigateTo(route) {
                this.$router.push("/" + route);
            }
        },
        created() {},
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
