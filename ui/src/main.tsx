import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import { App } from './App'
import { ErrorBoundary } from './ErrorPage'
import { HealthProvider } from './health'
import './app.css'

createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <HealthProvider><ErrorBoundary><App /></ErrorBoundary></HealthProvider>
  </StrictMode>,
)
