import { useState } from 'react'

export default function Login({ onLogin, onNavigate }) {
  const [formData, setFormData] = useState({ email: '', password: '' })
  const [error, setError] = useState('')
  const [isSubmitting, setIsSubmitting] = useState(false)

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

          <div className="form-group">
            <input
              type="password"
              name="password"
              className="auth-input"
              placeholder="Mật khẩu" aria-label="Mật khẩu"
              value={formData.password}
              onChange={handleChange}
              required
            />
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
