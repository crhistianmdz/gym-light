import { useState, type FormEvent } from 'react';
import { useAuth } from '@/contexts/AuthContext';
import type { LoginFormData, LoginFormErrors } from './LoginForm.types';

function validate(data: LoginFormData): LoginFormErrors {
  const errors: LoginFormErrors = {};
  if (!data.email.trim()) errors.email = 'El email es requerido';
  else if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(data.email))
    errors.email = 'El email no es válido';
  if (!data.password) errors.password = 'La contraseña es requerida';
  return errors;
}

export function LoginForm() {
  const { login } = useAuth();
  const [form, setForm]       = useState<LoginFormData>({ email: '', password: '' });
  const [errors, setErrors]   = useState<LoginFormErrors>({});
  const [isLoading, setIsLoading] = useState(false);

  const handleChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    setForm(prev => ({ ...prev, [e.target.name]: e.target.value }));
    setErrors(prev => ({ ...prev, [e.target.name]: undefined, form: undefined }));
  };

  const handleSubmit = async (e: FormEvent) => {
    e.preventDefault();
    const validationErrors = validate(form);
    if (Object.keys(validationErrors).length > 0) {
      setErrors(validationErrors);
      return;
    }

    setIsLoading(true);
    try {
      await login({ email: form.email, password: form.password });
    } catch (err) {
      setErrors({ form: err instanceof Error ? err.message : 'Error al iniciar sesión' });
    } finally {
      setIsLoading(false);
    }
  };

  return (
    <div style={{ maxWidth: 400, margin: '80px auto', padding: 24 }}>
      <h1 style={{ marginBottom: 24, textAlign: 'center' }}>GymFlow</h1>
      <form onSubmit={handleSubmit} noValidate>
        <div style={{ marginBottom: 16 }}>
          <label htmlFor="email">Email</label>
          <input
            id="email"
            name="email"
            type="email"
            value={form.email}
            onChange={handleChange}
            disabled={isLoading}
            style={{ display: 'block', width: '100%', marginTop: 4 }}
            autoComplete="email"
          />
          {errors.email && <span style={{ color: 'red', fontSize: 12 }}>{errors.email}</span>}
        </div>

        <div style={{ marginBottom: 16 }}>
          <label htmlFor="password">Contraseña</label>
          <input
            id="password"
            name="password"
            type="password"
            value={form.password}
            onChange={handleChange}
            disabled={isLoading}
            style={{ display: 'block', width: '100%', marginTop: 4 }}
            autoComplete="current-password"
          />
          {errors.password && <span style={{ color: 'red', fontSize: 12 }}>{errors.password}</span>}
        </div>

        {errors.form && (
          <div style={{ color: 'red', marginBottom: 16, textAlign: 'center' }}>
            {errors.form}
          </div>
        )}

        <button
          type="submit"
          disabled={isLoading}
          style={{ width: '100%', padding: 12 }}
        >
          {isLoading ? 'Ingresando...' : 'Ingresar'}
        </button>
      </form>
    </div>
  );
}