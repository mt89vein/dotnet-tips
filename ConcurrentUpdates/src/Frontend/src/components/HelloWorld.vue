<script setup lang="ts">
import { Centrifuge } from 'centrifuge'
import { ref, onMounted, computed, type Ref } from 'vue'

const productId = 1

const resourceUri = `http://localhost:5261/api/products/${productId}`

const centrifuge = new Centrifuge('ws://localhost:8000/connection/websocket')

// subscribe to product channel
const sub = centrifuge.newSubscription(`products:${productId}`)

sub.on('publication', (ctx: any) => updateProduct(ctx.data, true)).subscribe()

centrifuge.connect()

/**
 * Property class tracks modified values.
 */
class Property<T> {
  /**
   * Original (unmodified value).
   */
  originalValue: T | null

  /**
   * Current value. May be same as original value.
   */
  currentValue: T | null

  /**
   * Is property modified.
   */
  isModified: boolean

  /**
   * Is property changed on server while property was modified on client
   */
  hasConflict: boolean

  constructor(initialValue: T | null) {
    this.originalValue = initialValue
    this.currentValue = initialValue
    this.isModified = false
    this.hasConflict = false
  }

  reset() {
    this.currentValue = this.originalValue
    this.isModified = false
    this.hasConflict = false
  }

  updateIsModified() {
    this.isModified = this.currentValue != this.originalValue

    if (!this.isModified) {
      this.hasConflict = false
    }
  }

  updateHasConflict() {
    this.hasConflict = this.isModified
  }
}

type Product = {
  id: number
  version: number
  name: Property<string>
  description: Property<string>
  price: Property<number>
}

type ProductResponse = {
  id: number
  version: number
  name: string
  description: string | null
  price: number
}

const product = ref({
  id: 0,
  version: 0,
  description: new Property<string>(null!),
  name: new Property<string>(null!),
  price: new Property<number>(null!),
} as Product)

onMounted(async () => {
  const resp = await fetch(resourceUri)

  updateProduct(await resp.json(), false)
})

const onCancelChanges = () => iterateOverProperties((_, p) => p.reset())

const onSave = async () => {
  const resp = await fetch(resourceUri, {
    method: 'PATCH',
    headers: {
      Accept: 'application/json',
      'Content-Type': 'application/json',
    },
    body: toJsonWithMixedFields(product.value),
  })

  if (resp.status == 204) {
    window.alert('no changes saved')
  } else if (resp.status == 200) {
    updateProduct(await resp.json(), false)
  } else if (resp.status == 409) {
    updateProduct(await resp.json(), true)
  } else {
    window.alert('save failed')
  }
}

const toJsonWithMixedFields = (data: Record<string, Property<any> | any>): string => {
  const acc = {} as Record<string, any>
  iterateOverProperties(
    (propName, p) => {
      if (p.isModified) {
        acc[propName] = p.currentValue
      }
    },
    (propName, p) => (acc[propName] = p),
  )

  return JSON.stringify(acc)
}

const updateProduct = (productResponse: ProductResponse, markHasConflict: boolean) => {
  if (productResponse.version <= product.value.version) {
    return
  }

  product.value.id = productResponse.id
  product.value.version = productResponse.version

  iterateOverProperties((propName, p) => {
    const dynamicKey = propName as keyof ProductResponse
    const newServerValue = productResponse[dynamicKey]
    const modifiedOnServer = p.originalValue != newServerValue

    p.originalValue = newServerValue

    if (!p.isModified) {
      p.reset()
    } else {
      p.updateIsModified()

      if (modifiedOnServer && markHasConflict) {
        p.updateHasConflict()
      }
    }
  })
}

const iterateOverProperties = (
  callbackProp: (propName: string, property: Property<any>) => void,
  callbackOther?: (propName: string, property: any) => void,
) => {
  for (const [key, value] of Object.entries(product.value)) {
    if (
      value != null &&
      typeof value === 'object' &&
      'isModified' in value &&
      'currentValue' in value
    ) {
      callbackProp(key, value)
    } else {
      if (callbackOther) {
        callbackOther(key, value)
      }
    }
  }
}

const getClass = (prop: Property<any>): any => {
  return {
    conflict: prop.hasConflict,
    changed: !prop.hasConflict && prop.isModified,
  }
}
</script>

<template>
  <div>
    <pre>{{ product }}</pre>
    <h1>data on the server</h1>

    <h1 class="green">id: {{ product?.id || 'null' }}</h1>
    <h1 class="green">version: {{ product?.version || 'null' }}</h1>
    <h1 class="green">name: {{ product?.name.originalValue || 'null' }}</h1>
    <h1 class="green">description: {{ product?.description.originalValue || 'null' }}</h1>
    <h1 class="green">price: {{ product?.price.originalValue || 'null' }}</h1>

    <br />
    <br />
    <br />

    <h1>data on the client</h1>

    <h1 class="green">id: {{ product?.id || 'null' }}</h1>
    <h1 class="green">version: {{ product?.version || 'null' }}</h1>
    <h1 class="green" :class="getClass(product.name)">
      name: {{ product?.name.currentValue || 'null' }}
    </h1>
    <h1 class="green" :class="getClass(product.description)">
      description: {{ product?.description.currentValue || 'null' }}
    </h1>
    <h1 class="green" :class="getClass(product.price)">
      price: {{ product?.price.currentValue || 'null' }}
    </h1>
    <input
      v-model.string="product.name.currentValue"
      @input="product.name.updateIsModified"
    />
    <input
      v-model.string="product.description.currentValue"
      @input="product.description.updateIsModified"
    />
    <input
      v-model.number="product.price.currentValue"
      type="number"
      @input="product.price.updateIsModified"
    />
    <button @click="onCancelChanges">Cancel changes</button>
    <button @click="onSave">Save changes</button>
  </div>
</template>

<style scoped>
.changed {
  color: rgb(217, 145, 21);
}
.conflict {
  color: red;
}
</style>
