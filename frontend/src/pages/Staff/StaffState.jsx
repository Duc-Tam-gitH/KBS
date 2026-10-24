export function StaffState({ loading, error, reload }) {
  if (loading) return <p className="sf-state" role="status">Đang tải dữ liệu…</p>
  if (error) return <div className="sf-state" role="alert"><p>{error}</p>
    <button type="button" onClick={reload}>Thử lại</button></div>
  return null
}

export function StaffNotice({ notice }) {
  return notice ? <p className="sf-notice" role={notice.error ? 'alert' : 'status'}>{notice.text}</p> : null
}
