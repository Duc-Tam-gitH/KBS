import { useState } from 'react'
import { Eye, EyeOff } from 'lucide-react'

export default function Login({ onLogin, onNavigate }) {
  const [formData, setFormData] = useState({ email: '', password: '' })
  const [error, setError] = useState('')
  const [isSubmitting, setIsSubmitting] = useState(false)
  const [showPassword, setShowPassword] = useState(false)

  const handleChange = (event) => {
    setFormData({ ...formData, [event.target.name]: event.target.value })
  }

  const handleSubmit = async (event) => {
    event.preventDefault()
    setError('')
    setIsSubmitting(true)

    try {
      await onLogin({
        loginIdentifier: formData.email,
        password: formData.password,
      })
    } catch (submitError) {
      setError(submitError.message || 'Không thể đăng nhập.')
    } finally {
      setIsSubmitting(false)
    }
  }

  return (
    <div className="auth-container">
      <div className="auth-box">
        <p className="eyebrow">KBS / YOUR SPACE</p>
        <h1 className="auth-title">Chào mừng trở lại</h1>
        <p className="auth-subtitle">Đăng nhập vào tài khoản của bạn</p>

        <form onSubmit={handleSubmit}>
          <div className="form-group">
            <input
              type="text"
              name="email"
              className="auth-input"
              placeholder="Email hoặc tên đăng nhập" aria-label="Email hoặc tên đăng nhập"
              value={formData.email}
              onChange={handleChange}
              required
            />
          </div>

          <div className="form-group auth-password-field">
            <input
              type={showPassword ? 'text' : 'password'}
              name="password"
              className="auth-input"
              placeholder="Mật khẩu" aria-label="Mật khẩu"
              value={formData.password}
              onChange={handleChange}
              required
            />
            <button
              type="button"
              className="auth-password-toggle"
              aria-label={showPassword ? 'Ẩn mật khẩu' : 'Hiện mật khẩu'}
              onClick={() => setShowPassword((visible) => !visible)}
            >
              {showPassword ? <EyeOff size={20} aria-hidden="true" /> : <Eye size={20} aria-hidden="true" />}
            </button>
          </div>

          <button type="submit" className="auth-button" disabled={isSubmitting}>
            {isSubmitting ? 'Đang đăng nhập…' : 'Đăng nhập'}
          </button>
        </form>

        {error && <div className="auth-error" role="alert">{error}</div>}

        <button type="button" className="auth-link" onClick={() => onNavigate('register')}>
          Chưa có tài khoản? Đăng ký ngay
        </button>
      </div>
    </div>
  )
}
