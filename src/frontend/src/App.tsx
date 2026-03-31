import React from 'react'
import { ThemeProvider, createTheme, CssBaseline } from '@mui/material'
import { AuthProvider } from './contexts/AuthContext'
import AppRouter from './router'

const theme = createTheme({
  palette: {
    primary: { main: '#1976d2' },
    secondary: { main: '#dc004e' }
  }
})

const App: React.FC = () => {
  return (
    <ThemeProvider theme={theme}>
      <CssBaseline />
      <AuthProvider>
        <AppRouter />
      </AuthProvider>
    </ThemeProvider>
  )
}

export default App