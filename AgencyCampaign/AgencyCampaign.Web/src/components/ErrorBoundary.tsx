import { Component, type ErrorInfo, type ReactNode } from 'react'
import { Button } from 'archon-ui'

interface ErrorBoundaryProps {
  children: ReactNode
}

interface ErrorBoundaryState {
  hasError: boolean
}

class ErrorBoundary extends Component<ErrorBoundaryProps, ErrorBoundaryState> {
  state: ErrorBoundaryState = { hasError: false }

  static getDerivedStateFromError(): ErrorBoundaryState {
    return { hasError: true }
  }

  componentDidCatch(error: Error, errorInfo: ErrorInfo): void {
    console.error('Render error captured by ErrorBoundary', error, errorInfo)
  }

  handleReload = (): void => {
    window.location.reload()
  }

  render(): ReactNode {
    if (this.state.hasError) {
      return (
        <div className="flex min-h-screen flex-col items-center justify-center gap-4 p-6 text-center">
          <h1 className="text-xl font-semibold text-foreground">Algo deu errado</h1>
          <p className="max-w-md text-sm text-muted-foreground">
            Ocorreu um erro inesperado ao carregar esta tela. Tente recarregar a pagina. Se o problema persistir, entre em contato com o suporte.
          </p>
          <Button onClick={this.handleReload}>Recarregar</Button>
        </div>
      )
    }

    return this.props.children
  }
}

export default ErrorBoundary
