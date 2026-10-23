import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import './index.css'
import './states.css'
import './components/layout/layout.css'
import App from './App.jsx'

createRoot(document.getElementById('root')).render(
  <StrictMode>
    <App></App>
  </StrictMode>,
)
