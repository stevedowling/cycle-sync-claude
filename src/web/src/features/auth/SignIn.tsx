import { useState, type FormEvent } from 'react';
import { useAppDispatch } from '../../app/hooks';
import { useSignInMutation } from '../api/apiSlice';
import { credentialsReceived } from './authSlice';

/**
 * Development sign-in. It exchanges an email for a session by posting it to the auth endpoint,
 * which pairs with the offline Google validator registered in Development and E2E (see the API's
 * Program.cs). In production this is replaced by the Google OIDC button; the resulting session
 * token flows through the same slice.
 */
export function SignIn() {
  const dispatch = useAppDispatch();
  const [signIn, { isLoading, error }] = useSignInMutation();
  const [email, setEmail] = useState('');

  const onSubmit = async (event: FormEvent) => {
    event.preventDefault();
    const result = await signIn({ idToken: email.trim() }).unwrap();
    dispatch(credentialsReceived(result));
  };

  return (
    <section className="card" aria-labelledby="signin-heading">
      <h2 id="signin-heading" className="section-title">
        Sign in
      </h2>
      <form onSubmit={onSubmit} className="signin-form">
        <label htmlFor="email">Work email</label>
        <input
          id="email"
          type="email"
          required
          value={email}
          onChange={(e) => setEmail(e.target.value)}
          placeholder="you@cyclesync.example"
          data-testid="signin-email"
        />
        <button type="submit" disabled={isLoading} data-testid="signin-submit">
          {isLoading ? 'Signing in…' : 'Sign in'}
        </button>
        {error && (
          <p className="status-error" role="alert" data-testid="signin-error">
            Sign-in failed. Check the email domain is permitted.
          </p>
        )}
      </form>
    </section>
  );
}
