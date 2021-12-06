<template>  
  <transition name="modal">
    <div class="modal-mask">
      <div class="modal-dialog">
        <div class="modal-content">
          <div class="modal-header">
            <h3 class="modal-title" ><slot name="header">default header</slot></h3>              
          </div>
          <div class="modal-body">
            <p><slot name="body">default body</slot></p>
          </div>
          <div class="modal-footer">
            <slot name="footer">
              <button class="btn" @click="$emit('no')"> {{ noButton }}</button>
              <button class="btn" @click="$emit('yes')">{{ yesButton }}</button>           
            </slot>
          </div>
        </div>
      </div>
    </div>
  </transition>
</template>


<script>
  export default {
    props: {
        yesButton: {
          type: String,
          default: "Yes",
        },
        noButton: {
          type: String,
          default: "No",
        }        
      },
  }

</script>        