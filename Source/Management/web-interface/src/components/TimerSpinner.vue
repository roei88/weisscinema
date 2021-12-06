<template>
    <svg v-if="show" :width="sqSize" :height="sqSize" :viewBox="viewBox">
        <circle
            class="circle-background"
            :cx="sqSize / 2"
            :cy="sqSize / 2"
            :r="radius"
            :stroke-width="`${strokeWidth}px`"
        />
        <circle
            class="circle-progress"
            :cx="sqSize / 2"
            :cy="sqSize / 2"
            :r="radius"
            :stroke-width="`${strokeWidth}px`"
            :transform="`rotate(-90 ${sqSize / 2} ${sqSize / 2})`"
            :style="{ strokeDasharray: dashArray, strokeDashoffset: dashOffset }"
        />
        <circle
             class="circle-dashes"
             :cx="sqSize / 2"
             :cy="sqSize / 2"
             :r="radius"
             :stroke-width="`${strokeWidth}px`"
             :style="{ strokeDasharray: '3.14' }"
        />
    </svg>
</template>

<script>
export default {
    name: 'timerSpinner',
    props: {
        show: { type: Boolean },
        strokeWidth: { type: Number, default: 10 },
        sqSize: { type: Number, default: 70 },
        timeout: { type: Number, default: 60000 }, // milliseconds
        infinite: { type: Boolean, default: false }
    },
    data () {
        return {
            percentage: 0,
            timer: null
        }
    },
    mounted () {
        this.show && this.start(this.infinite);
    },
    beforeDestroy () {
        this.timer && clearInterval(this.timer);
    },
    computed: {
        radius () {
            return (this.sqSize - this.strokeWidth) / 2;
        },
        viewBox () {
            return `0 0 ${this.sqSize} ${this.sqSize}`
        },
        dashArray () {
            return this.radius * Math.PI * 2;
        },
        dashOffset () {
            return this.dashArray - this.dashArray * this.percentage / 100;
        }
    },
    watch: {
        show (value) {
            this.reset();

            value && this.start(this.infinite);
        }
    },
    methods: {
        start (infinite) {
            const interval = infinite ? 30 : this.timeout / 60;
            const increaseValue = 100 / 60;

            this.timer = setInterval(() => {
                if (this.percentage >= 100 && !infinite) {
                    this.stop();

                    this.$emit('onTimerEnd');
                    return;
                }

                this.percentage = (this.percentage + increaseValue) % 200;
            }, interval)
        },
        reset () {
            this.stop();
            this.percentage = 0;
        },
        stop () {
            clearInterval(this.timer);
        }
    }
}
</script>

<style scoped>
    .circle-background, .circle-progress, .circle-dashes {
        fill: none;
    }

    .circle-background {
        stroke: #c9c9c9;
    }

    .circle-dashes {
        stroke: #fff;
    }

    .circle-progress {
        stroke: #3596b1cc;
    }
</style>
