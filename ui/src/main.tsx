import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import { Departments } from './Departments'
import './departments.css'

createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <Departments />
  </StrictMode>,
)
