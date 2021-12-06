<template>
   <div class="counter"> {{ time }} </div>
</template>

<script>
const moment = require('moment');

export default {
    name: "countDownComponent",
    props: {
        count: {
            type: Number,
            default: 0
        }
    },
    data() {
        return {
            interval: null,
            time: null,
            dateTime: null
        }
    },
    watch: {
        count() {
            if (this.count > 0) {
                this.startCounter();
            } else {
                this.stopCounter();
            }
        }
    },
    computed: {
        currentData() {
            return moment(60 * this.count * 1000);
        }
    },
    beforeDestroy () {
       this.stopCounter();
    },
    methods: {
        startCounter() {
            this.stopCounter();

            this.interval = setInterval(() => {
                this.dateTime = this.currentData;
                this.dateTime = moment(this.dateTime.subtract(1, 'seconds'));
                this.dateTime.set("hour", 0);
                this.time = this.dateTime.format('HH:mm:ss');

                if (this.dateTime.minutes() === 0 && this.dateTime.seconds() === 0) {
                    this.stopCounter();

                    this.$emit("timerTimeIsOver");
                }
            }, 1000);
        },
        stopCounter() {
            clearInterval(this.interval);
            this.time = null;
        }
    }
}
</script>
