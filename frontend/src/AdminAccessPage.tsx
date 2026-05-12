import React, { useState } from "react";
import { useAdminSession } from "./AdminSessionContext";

const AdminAccessPage: React.FC = () => {
  const { hasSession, expiresAtUtc, signIn, signOut } = useAdminSession();
  const [adminKey, setAdminKey] = useState("");
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    setError(null);
    setLoading(true);
    try {
      await signIn(adminKey);
      setAdminKey("");
    } catch (err) {
      console.error(err);
      setError("Could not obtain an admin session. Check the admin key and that the backend is reachable.");
    } finally {
      setLoading(false);
    }
  }

  const expiryLabel =
    hasSession && expiresAtUtc
      ? new Date(expiresAtUtc).toLocaleString(undefined, {
          dateStyle: "medium",
          timeStyle: "short"
        })
      : null;

  return (
    <div className="space-y-6">
      <header className="space-y-1">
        <h1 className="text-xl font-semibold tracking-tight text-slate-900">Admin access</h1>
        <p className="text-sm text-slate-500">
          Enter the server&apos;s <code className="text-xs">AUTH_SETTINGS_ADMIN_KEY</code> once to
          receive a short-lived session token. Other tabs then use that session automatically until
          it expires or you sign out.
        </p>
      </header>

      {hasSession ? (
        <div className="rounded-lg border border-emerald-200 bg-emerald-50 px-4 py-3 text-sm text-emerald-900 space-y-2">
          <p>
            Admin session is active
            {expiryLabel ? (
              <>
                {" "}
                until <span className="font-medium">{expiryLabel}</span> (local time).
              </>
            ) : (
              "."
            )}
          </p>
          <button
            type="button"
            onClick={() => signOut()}
            className="inline-flex rounded-md border border-emerald-300 bg-white px-3 py-1.5 text-xs font-medium text-emerald-900 hover:bg-emerald-100"
          >
            Sign out admin session
          </button>
        </div>
      ) : null}

      <form onSubmit={(e) => void handleSubmit(e)} className="space-y-4 max-w-md">
        <div className="space-y-2">
          <label htmlFor="admin-key" className="block text-sm font-medium text-slate-700">
            Admin key
          </label>
          <input
            id="admin-key"
            type="password"
            autoComplete="off"
            className="w-full rounded-md border border-slate-300 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-sky-500"
            placeholder="AUTH_SETTINGS_ADMIN_KEY value"
            value={adminKey}
            onChange={(e) => setAdminKey(e.target.value)}
          />
        </div>
        {error ? <p className="text-sm text-red-600">{error}</p> : null}
        <button
          type="submit"
          disabled={loading || !adminKey.trim()}
          className="inline-flex items-center justify-center rounded-lg bg-sky-600 px-4 py-2 text-sm font-medium text-white shadow-sm hover:bg-sky-700 disabled:opacity-50"
        >
          {loading ? "Requesting…" : "Request admin session"}
        </button>
      </form>

      <p className="text-xs text-slate-500 max-w-prose">
        The session token is stored in <strong>session storage</strong> for this tab only and is
        sent as the <code className="text-xs">X-Admin-Session</code> header on admin API calls. You
        can still use <code className="text-xs">X-Admin-Key</code> from scripts; the UI uses the
        session flow.
      </p>
    </div>
  );
};

export default AdminAccessPage;
