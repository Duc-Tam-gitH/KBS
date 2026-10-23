import { useState } from 'react'
import './Auth.css'

export default function Register({ onRegister, onNavigate }) {
  const [formData, setFormData] = useState({
    fullName: '',
    username: '',
    email: '',
    password: '',
    confirmPassword: '',
  })
  const [error, setError] = useState('')
  const [isSubmitting, setIsSubmitting] = useState(false)

  const handleChange = (event) => {
    setFormData({ ...formData, [event.target.name]: event.target.value })
  }

  const handleSubmit = async (event) => {
    event.preventDefault()
    setError('')

    if (formData.password !== formData.confirmPassword) {
      setError('Mật khẩu xác nhận không khớp.')
      return
    }

    setIsSubmitting(true)

    try {
      await onRegister(formData)
      onNavigate('auth')
    } catch (submitError) {
      setError(submitError.message || 'Không thể tạo tài khoản.')
    } finally {
      setIsSubmitting(false)
    }
  }

  return (
    <div className="auth-container">
      <div className="auth-box">
        <h1 className="auth-title">Đăng ký tài khoản</h1>
        <p className="auth-subtitle">Tạo tài khoản của bạn</p>

        <form onSubmit={handleSubmit}>
          <div className="form-group">
            <input
              type="text"
              name="fullName"
              className="auth-input"
              placeholder="Họ và tên"
              value={formData.fullName}
              onChange={handleChange}
            />
          </div>
          <div className="form-group">
            <input
              type="text"
              name="username"
              className="auth-input"
              placeholder="Username"
              value={formData.username}
              onChange={handleChange}
              required
              minLength="3"
            />
          </div>
          <div className="form-group">
            <input
              type="email"
              name="email"
              className="auth-input"
              placeholder="Địa chỉ email"
              value={formData.email}
              onChange={handleChange}
              required
            />
          </div>
          <div className="form-group">
            <input
              type="password"
              name="password"
              className="auth-input"
              placeholder="Mật khẩu (tối thiểu 8 ký tự chứa chữ cái và số)"
              value={formData.password}
              onChange={handleChange}
              required
              minLength="8"
            />
          </div>
          <div className="form-group">
            <input
              type="password"
              name="confirmPassword"
              className="auth-input"
              placeholder="Xác nhận mật khẩu"
              value={formData.confirmPassword}
              onChange={handleChange}
              required
              minLength="8"
            />
          </div>

          <button type="submit" className="auth-button" disabled={isSubmitting}>
            {isSubmitting ? 'Creating account…' : 'Đăng ký 🥂'}
          </button>
        </form>

        {error && <div className="auth-error">{error}</div>}

        <button type="button" className="auth-link" onClick={() => onNavigate('auth')}>
          Đã có tài khoản? Đăng nhập
        </button>
      </div>
    </div>
  )
}
