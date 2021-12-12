// The Vue build version to load with the `import` command
// (runtime-only or standalone) has been set in webpack.base.conf with an alias.
import Vue from 'vue'
import VueRouter from 'vue-router'
import App from './App'
import 'vue-awesome/icons'
import Vuetify from "vuetify";
import "vuetify/dist/vuetify.min.css";
import NavBar from './components/NavBar.vue'
import StatusPage from './components/StatusPage.vue'
import HoverFrame from './components/HoverFrame.vue'
import Icon from 'vue-awesome/components/Icon'
import VueCookies from 'vue-cookies'
import _ from "lodash";
import {store} from './store';
import jQuery from 'jquery'
import './assets/css/font-awesome.min.css';
import 'bootstrap/less/bootstrap.less'
import './assets/css/sb-admin-2.css';
import './assets/css/main.css';
import './assets/css/costom.css';
import './assets/css/costom_font.css';
import './assets/css/movie_card.css';

global.jQuery = jQuery
let Bootstrap = require('bootstrap')

Vue.config.productionTip = false

export const eventBus = new Vue({
  methods: {
    isNumber: function(evt, isTime) {
        evt = (evt) ? evt : window.event;
        var charCode = (evt.which) ? evt.which : evt.keyCode;
        
        var condition = (charCode > 31 && (charCode < 48 || charCode > 57))
        if (isTime != undefined && isTime == true)
        {
            condition = condition && charCode !== 58;
        }

        if (condition) {
            evt.preventDefault();
        } else {
            return true;
        }
    }
  }
});

Vue.use(VueRouter);
Vue.use(VueCookies);
Vue.use(Vuetify);

Vue.component('icon', Icon)
Vue.component('costom-nav-bar', NavBar)
Vue.component('status-page', StatusPage)
Vue.component('hover-frame', HoverFrame)

const routes = [
    { path: '/',                      name: 'status-page',                  component: StatusPage }
  ];
  
const router = new VueRouter({  
    routes 
  });
  
new Vue({
  vuetify : new Vuetify(),
  el: '#app',
  store,
  router,
  render: h => h(App)
})
