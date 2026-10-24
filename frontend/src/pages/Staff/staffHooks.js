import { useEffect, useRef, useState } from 'react'
import { api } from '../../services/api'

export const money = (value) => new Intl.NumberFormat('vi-VN', {
  style: 'currency', currency: 'VND',
}).format(value)
export const dateTime = (value) => value ? new Date(value).toLocaleString('vi-VN') : '—'
export const statusLabels = {
  PENDING: 'Mới', CONFIRMED: 'Đang xử lý', SHIPPING: 'Đang giao', COMPLETED: 'Hoàn thành', CANCELLED: 'Đã hủy',
}
export const nextStatuses = {
  PENDING: ['CONFIRMED', 'CANCELLED'], CONFIRMED: ['SHIPPING', 'CANCELLED'], SHIPPING: ['COMPLETED'],
}

export function useStaffData(resource) {
  const [retry, setRetry] = useState(0)
  const [result, setResult] = useState(null)
  const key = `${resource}:${retry}`
  useEffect(() => {
    let active = true
    const request = resource === 'orders' ? api.staff.orders() : api.staff.get(resource)
    request.then((data) => { if (active) setResult({ key, data }) })
      .catch((error) => { if (active) setResult({ key, error: error.message }) })
    return () => { active = false }
  }, [resource, key])
  return {
    data: result?.key === key ? result.data : null,
    error: result?.key === key ? result.error : '',
    loading: result?.key !== key,
    reload: () => setRetry((value) => value + 1),
  }
}

export function useStaffAction() {
  const [busy, setBusy] = useState(false)
  const [notice, setNotice] = useState(null)
  const locked = useRef(false)
  const mounted = useRef(true)
  useEffect(() => {
    mounted.current = true
    return () => { mounted.current = false }
  }, [])
  const run = async (operation, message = 'Đã lưu thay đổi.') => {
    if (locked.current) return false
    locked.current = true
    setBusy(true)
    setNotice(null)
    try {
      await operation()
      if (mounted.current && message) setNotice({ text: message })
      return mounted.current
    } catch (error) {
      if (mounted.current) setNotice({ error: true, text: error.message || 'Thao tác thất bại.' })
      return false
    } finally {
      locked.current = false
      if (mounted.current) setBusy(false)
    }
  }
  return { busy, notice, run }
}
