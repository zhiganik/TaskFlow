import { zodResolver } from '@hookform/resolvers/zod'
import { useState } from 'react'
import { useForm } from 'react-hook-form'
import { Link, useLocation, useNavigate } from 'react-router-dom'
import { getErrorMessage, getFieldErrors } from '../api/errors'
import { AuthLayout } from '../components/layout/AuthLayout'
import { Alert } from '../components/ui/Alert'
import { Button } from '../components/ui/Button'
import { TextField } from '../components/ui/TextField'
import { useLogin } from '../hooks/useAuth'
import { loginSchema, type LoginFormValues } from '../validation/auth.schema'

export function LoginPage() {
  const navigate = useNavigate()
  const location = useLocation()
  const loginMutation = useLogin()
  const [serverError, setServerError] = useState<string | null>(null)

  const justRegistered = (location.state as { registered?: boolean } | null)?.registered ?? false

  const {
    register,
    handleSubmit,
    setError,
    formState: { errors },
  } = useForm<LoginFormValues>({ resolver: zodResolver(loginSchema) })

  const onSubmit = (values: LoginFormValues) => {
    setServerError(null)
    loginMutation.mutate(values, {
      onSuccess: () => navigate('/', { replace: true }),
      onError: (error) => {
        const fieldErrors = getFieldErrors(error)
        if (fieldErrors) {
          for (const [field, message] of Object.entries(fieldErrors)) {
            setError(field as keyof LoginFormValues, { message })
          }
          return
        }
        setServerError(getErrorMessage(error))
      },
    })
  }

  return (
    <AuthLayout
      title="Log in to TaskFlow"
      footer={
        <>
          Don&apos;t have an account?{' '}
          <Link to="/register" className="font-medium text-brand-700 hover:underline">
            Sign up
          </Link>
        </>
      }
    >
      <form onSubmit={handleSubmit(onSubmit)} className="space-y-4" noValidate>
        {serverError ? (
          <Alert variant="error">{serverError}</Alert>
        ) : justRegistered ? (
          <Alert variant="success">Account created. Log in to continue.</Alert>
        ) : null}

        <TextField
          label="Email"
          type="email"
          autoComplete="email"
          placeholder="you@example.com"
          error={errors.email?.message}
          {...register('email')}
        />

        <TextField
          label="Password"
          type="password"
          autoComplete="current-password"
          placeholder="••••••••"
          error={errors.password?.message}
          {...register('password')}
        />

        <Button type="submit" className="w-full" loading={loginMutation.isPending}>
          {loginMutation.isPending ? 'Logging in…' : 'Log in'}
        </Button>
      </form>
    </AuthLayout>
  )
}
