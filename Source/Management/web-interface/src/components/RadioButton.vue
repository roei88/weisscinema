<template>
    <div>
        <input :id="id" type="radio" :value="value" :name="name" v-model="radioButtonValue" :checked="isChecked">
        <label :for="id">{{ label }}</label>
    </div>
</template>

<script>

export default {
    props: ['id', 'name', 'label', 'value', 'isChecked'],
    computed: {
        radioButtonValue: {
            get: function() {
                return this.value
            },
            set: function() {
                this.$emit("change", this.value)
            }
        }
    }    
}
</script>