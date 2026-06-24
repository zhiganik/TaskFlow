import { zodResolver } from '@hookform/resolvers/zod'
import { useState } from 'react'
import { useForm } from 'react-hook-form'
import { Link, useNavigate } from 'react-router-dom'
import { getErrorMessage, getFieldErrors } from '../api/errors'
import { AuthLayout } from '../components/layout/AuthLayout'
import { Alert } from '../components/ui/Alert'
import { Button } from '../components/ui/Button'
import { TextField } from '../components/ui/TextField'
import { useRegister } from '../hooks/useAuth'
import { registerSchema, type RegisterFormValues } from '../validation/auth.schema'

export function RegisterPage() {
  const navigate = useNavigate()
  const registerMutation = useRegister()
  const [serverError, setServerError] = useState<string | null>(null)

  const {
    register,
    handleSubmit,
    setError,
    formState: { errors },
  } = useForm<RegisterFormValues>({ resolver: zodResolver(registerSchema) })

  const onSubmit = (values: RegisterFormValues) => {
    setServerError(null)
    registerMutation.mutate(values, {
      onSuccess: () => navigate('/login', { replace: true, state: { registered: true } }),
      onError: (error) => {
        const fieldErrors = getFieldErrors(error)
        if (fieldErrors) {
          for (const [field, message] of Object.entries(fieldErrors)) {
            setError(field as keyof RegisterFormValues, { message })
          }
          return
        }
        setServerError(getErrorMessage(error))
      },
    })
  }

  return (
    <AuthLayout
      title="Create your account"
      subtitle="Start organizing your team's work"
      footer={
        <>
          Already have an account?{' '}
          <Link to="/login" className="font-medium text-brand-700 hover:underline">
            Log in
          </Link>
        </>
      }
    >
      <form onSubmit={handleSubmit(onSubmit)} className="space-y-4" noValidate>
        {serverError && <Alert variant="error">{serverError}</Alert>}

        <TextField
          label="Display name"
          autoComplete="name"
          placeholder="Jane Smith"
          error={errors.displayName?.message}
          {...register('displayName')}
        />

        <TextField
          label="Email"
          type="email"
          autoComplete="email"
          placeholder="you@example.com"
          error={errors.email?.message}
          {...register('email')}
        />

        <div className="space-y-1.5">
          <TextField
            label="Password"
            type="password"
            autoComplete="new-password"
            placeholder="••••••••"
            error={errors.password?.message}
            {...register('password')}
          />
          {!errors.password && (
            <p className="text-xs text-gray-400">
              At least 8 characters, with one uppercase letter and one number.
            </p>
          )}
        </div>

        <Button type="submit" className="w-full" loading={registerMutation.isPending}>
          {registerMutation.isPending ? 'Creating account…' : 'Create account'}
        </Button>
      </form>
    </AuthLayout>
  )
}
