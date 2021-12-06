<template>
    <div :class="mainClass">
        <div class="message-container">
            <div v-if="showSpinner" class="loading-spinner"></div>
            <span class="message">{{message}}</span>
        </div>
        <i v-if="showCloseButton" class="close-icon" @click="onClose"></i>
    </div>
</template>

<script>
export default {
    name: 'WarningPopup',
    props: {
        show: {
            type: Boolean,
            default: true
        },
        message: String,
        showCloseButton: {
            type: Boolean,
            default: false
        },
        showSpinner: {
            type: Boolean,
            default: false
        }
    },
    computed: {
        mainClass () {
            return {
                'warning-popup-container': true,
                active: this.show
            }
        }
    },
    methods: {
        onClose() {
            this.$emit('onClose')
        }
    }
}
</script>

<style scoped>
    .warning-popup-container {
        display: flex;
        background: #cb1616;
        height: 0;
        opacity: 0;
        width: 100%;
        padding: 0 20px;
        align-items: center;
        justify-content: space-between;
        transition: all .3s ease-in-out;
    }

    .warning-popup-container.active {
        height: 40px;
        opacity: 1;
    }

    .message-container {
        display: flex;
        align-items: center;
    }

    .message-container .loading-spinner {
        width: 30px;
        height: 30px;
        background: url("../assets/img/loading-spinner-dashed.gif") no-repeat;
        background-size: 100%;
        margin-right: 10px;
    }

    .message-container .message {
        font-size: 16px;
        color: #ffffff;
    }

    .warning-popup-container .btn {
        background-color: #fbfbfb;
        padding: 3px 15px;
    }

    .warning-popup-container .close-icon {
        width: 14px;
        height: 14px;
        cursor: pointer;
        background: url('../assets/img/close.svg') no-repeat;
    }
</style>
