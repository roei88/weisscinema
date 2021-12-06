// The Vue build version to load with the `import` command
// (runtime-only or standalone) has been set in webpack.base.conf with an alias.
import Vue from 'vue'
import VueRouter from 'vue-router'
import App from './App'
import 'vue-awesome/icons'


import RadioButton from './components/RadioButton.vue'

import NavBar from './components/NavBar.vue'
import StatusPage from './components/StatusPage.vue'
import ModalDialog from './components/ModalDialog.vue'
import OneButtonModalDialog from './components/OneButtonModalDialog.vue'

import Icon from 'vue-awesome/components/Icon'
import VueCookies from 'vue-cookies'
import vueSlider from 'vue-slider-component'

import "opencv.js";
import _ from "lodash";

import {store} from './store';

// import router from './router'
import jQuery from 'jquery'

global.jQuery = jQuery
let Bootstrap = require('bootstrap')

// import 'bootstrap/dist/css/bootstrap.css'
import './assets/css/font-awesome.min.css';
import 'bootstrap/less/bootstrap.less'

import './assets/css/sb-admin-2.css';
import './assets/css/main.css';
import './assets/css/costom.css';
import './assets/css/costom_font.css';
// import 'bootstrap/less/modals.less'

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

Vue.component('icon', Icon)
Vue.component('radio-button', RadioButton)

Vue.component('costom-nav-bar', NavBar)
Vue.component('status-page', StatusPage)
Vue.component('costom-modal-dialog', ModalDialog)
Vue.component('one-button-modal-dialog', OneButtonModalDialog)
Vue.component('vueslider', vueSlider)


const routes = [
    { path: '/',                      name: 'status-page',                  component: StatusPage }
  ];
  
const router = new VueRouter({  
    // mode: 'history',
    routes 
  });
  
new Vue({
  el: '#app',
  store,
  router,
  render: h => h(App),
  created() {
    if (this.$route.query.advanced == "true")
    {
      console.log("Advanced mode is on");
      this.$store.commit('setAdvancedMode', true);
    }
  }
})

router.beforeEach((to, from, next) => {
  var hasTo = (to != null);
  var isAdvancedRoute = false;
  var advancedRoutes = store.getters.getAdvancedRoutes;
  for (var i = 0; i < advancedRoutes.length; i++)
  {
      if (to.name == advancedRoutes[i])
      {
        isAdvancedRoute = true;
        break;
      }
  }
  var inAdvancedMode = store.getters.getAdvancedMode;
  if (hasTo && isAdvancedRoute && !inAdvancedMode)
  {
    console.log(to.name + " is accessible only in advanced route, routing to main page");
    return next("/");
  }
  return next();
});