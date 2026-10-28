import { useEffect, useState } from 'react'

const baseUrl = 'https://provinces.open-api.vn/api/v1'

function useRegions(path, field) {
  const [result, setResult] = useState(null)
  const [attempt, setAttempt] = useState(0)
  useEffect(() => {
    if (!path) return
    const controller = new AbortController()
    const timeout = setTimeout(() => controller.abort(), 15000)
    let active = true
    fetch(`${baseUrl}${path}`, { signal: controller.signal })
      .then(async response => {
        if (!response.ok) throw new Error('Không thể tải khu vực giao hàng.')
        const data = await response.json()
        const items = field ? data[field] : data
        if (!Array.isArray(items)) throw new Error('Dữ liệu khu vực không hợp lệ.')
        if (active) setResult({ path, attempt, items })
      })
      .catch(() => {
        if (active) setResult({ path, attempt, items: [], error: 'Không tải được danh sách. Vui lòng thử lại.' })
      })
      .finally(() => clearTimeout(timeout))
    return () => { active = false; clearTimeout(timeout); controller.abort() }
  }, [path, field, attempt])
  const current = result?.path === path && result?.attempt === attempt ? result : null
  return { items: current?.items || [], loading: Boolean(path && !current), error: current?.error,
    retry: () => setAttempt(value => value + 1) }
}

export default function ShippingAddress({ onChange, disabled = false }) {
  const [province, setProvince] = useState('')
  const [district, setDistrict] = useState('')
  const [ward, setWard] = useState('')
  const provinces = useRegions('/', null)
  const districts = useRegions(province ? `/p/${province}?depth=2` : null, 'districts')
  const wards = useRegions(district ? `/d/${district}?depth=2` : null, 'wards')
  const selectProvince = value => {
    setProvince(value); setDistrict(''); setWard(''); onChange('')
  }
  const selectDistrict = value => { setDistrict(value); setWard(''); onChange('') }
  const selectWard = value => {
    setWard(value)
    const selected = [wards.items.find(item => String(item.code) === value),
      districts.items.find(item => String(item.code) === district),
      provinces.items.find(item => String(item.code) === province)]
    onChange(selected.every(Boolean) ? selected.map(item => item.name).join(', ') : '')
  }
  return <div className="co-wide co-address">
    <div className="co-region-grid">
      {[
        ['province', 'Tỉnh / Thành phố', province, provinces, selectProvince, false],
        ['district', 'Quận / Huyện', district, districts, selectDistrict, !province],
        ['ward', 'Phường / Xã', ward, wards, selectWard, !district],
      ].map(([name, label, value, state, change, blocked]) => <div key={name}>
        <label>{label}<select name={name} required value={value}
          disabled={disabled || blocked || state.loading || Boolean(state.error) || !state.items.length}
          onChange={event => change(event.target.value)}>
          <option value="">{state.loading ? 'Đang tải…' : `Chọn ${label.toLocaleLowerCase('vi')}`}</option>
          {state.items.map(item => <option key={item.code} value={item.code}>{item.name}</option>)}
        </select></label>
        {state.error && <p className="co-region-error" role="alert">{state.error}
          <button type="button" disabled={disabled} onClick={state.retry}>Thử lại</button></p>}
        {!blocked && !state.loading && !state.error && !state.items.length &&
          <p className="co-region-error">Chưa có dữ liệu khu vực.</p>}
      </div>)}
    </div>
    <p className="co-address-note">Danh mục địa chỉ theo dữ liệu hành chính trước tháng 7/2025.</p>
    <label>Địa chỉ cụ thể<input name="address" autoComplete="street-address" required maxLength={300}
      disabled={disabled} placeholder="Số nhà, tên đường, tòa nhà" /></label>
  </div>
}
