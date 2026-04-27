import React, { useEffect, useState } from "react";
import type { AuthMode, AuthSettings, UpdateAuthSettingsRequest } from "./api";
import { getAuthSettings, updateAuthSettings } from "./api";

type FormState = {
  mode: AuthMode;
  bearerToken: string;
  oAuthClientId: string;
  oAuthClientSecret: string;
  accessTokenLifetimeMinutes: number;
  refreshTokenLifetimeDays: number;
  adminKey: string;
};

const defaultState: FormState = {
  mode: "None",
  bearerToken: "",
  oAuthClientId: "",
  oAuthClientSecret: "",
  accessTokenLifetimeMinutes: 60,
  refreshTokenLifetimeDays: 30,
  adminKey: ""
};

function mask(value: string, visible: number = 4): string {
  if (!value) return "";
  if (value.length <= visible) return value;
  const maskedLength = Math.max(0, value.length - visible);
  return "•".repeat(maskedLength) + value.slice(-visible);
}

const AuthSettingsPage: React.FC = () => {
  const [form, setForm] = useState<FormState>(defaultState);
  const [loading, setLoading] = useState(false);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState<string | null>(null);
  const [showSecrets, setShowSecrets] = useState(false);
  const [hasAnyTokens, setHasAnyTokens] = useState(false);

  useEffect(() => {
    void loadSettings();
  }, []);

  async function loadSettings() {
    try {
      setLoading(true);
      setError(null);
      const settings = await getAuthSettings(form.adminKey || undefined);
      applySettingsToForm(settings);
      setHasAnyTokens(settings.hasAnyTokens);
    } catch (err) {
      console.error(err);
      setError("Unable to load authentication settings. Check the admin key and backend.");
    } finally {
      setLoading(false);
    }
  }

  function applySettingsToForm(settings: AuthSettings) {
    setForm((prev) => ({
      ...prev,
      mode: settings.mode,
      bearerToken: settings.bearerToken ?? "",
      oAuthClientId: settings.oAuthClientId ?? "",
      oAuthClientSecret: settings.oAuthClientSecret ?? "",
      accessTokenLifetimeMinutes: settings.accessTokenLifetimeMinutes,
      refreshTokenLifetimeDays: settings.refreshTokenLifetimeDays
    }));
  }

  function handleChange<K extends keyof FormState>(key: K, value: FormState[K]) {
    setForm((prev) => ({ ...prev, [key]: value }));
  }

  function generateRandomToken(length: number = 32): string {
    const charset = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
    let result = "";
    const randomValues = crypto.getRandomValues(new Uint32Array(length));
    for (let i = 0; i < length; i += 1) {
      result += charset[randomValues[i] % charset.length] ?? "";
    }
    return result;
  }

  function handleGenerateBearer() {
    handleChange("bearerToken", generateRandomToken(40));
  }

  function handleGenerateClient() {
    handleChange("oAuthClientId", `client_${generateRandomToken(16)}`);
    handleChange("oAuthClientSecret", generateRandomToken(40));
  }

  async function handleSubmit(event: React.FormEvent) {
    event.preventDefault();
    try {
      setSaving(true);
      setError(null);
      setSuccess(null);

      const payload: UpdateAuthSettingsRequest = {
        mode: form.mode,
        bearerToken:
          form.mode === "Bearer" || form.mode === "CCAPIKey" ? form.bearerToken : undefined,
        oAuthClientId: form.mode === "OAuth" ? form.oAuthClientId : undefined,
        oAuthClientSecret: form.mode === "OAuth" ? form.oAuthClientSecret : undefined,
        accessTokenLifetimeMinutes: form.accessTokenLifetimeMinutes,
        refreshTokenLifetimeDays: form.refreshTokenLifetimeDays
      };

      const updated = await updateAuthSettings(payload, form.adminKey || undefined);
      applySettingsToForm(updated);
      setHasAnyTokens(updated.hasAnyTokens);
      setSuccess("Authentication settings saved.");
    } catch (err) {
      console.error(err);
      setError("Unable to save authentication settings. Check the admin key and backend.");
    } finally {
      setSaving(false);
    }
  }

  function copyToClipboard(text: string) {
    if (!navigator.clipboard) return;
    void navigator.clipboard.writeText(text);
  }

  return (
    <div className="space-y-6">
      <header className="space-y-1">
        <h1 className="text-xl font-semibold tracking-tight text-slate-900">
          Authentication Settings
        </h1>
        <p className="text-sm text-slate-500">
          Configure how external clients authenticate against the Mock Health System API.
        </p>
      </header>

      <form onSubmit={handleSubmit} className="space-y-6">
          <section className="space-y-3">
            <div className="flex flex-col gap-2 sm:flex-row sm:items-center sm:justify-between">
              <div>
                <h2 className="text-sm font-medium text-slate-700">Admin access</h2>
                <p className="text-xs text-slate-500">
                  If the backend is configured with <code>AUTH_SETTINGS_ADMIN_KEY</code>, you must
                  supply it here to read and update settings.
                </p>
              </div>
            </div>
            <div className="flex flex-col sm:flex-row sm:items-center gap-3">
              <input
                type="password"
                className="flex-1 rounded-md border border-slate-300 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-sky-500"
                placeholder="Admin key (optional in local dev)"
                value={form.adminKey}
                onChange={(e) => handleChange("adminKey", e.target.value)}
              />
              <button
                type="button"
                className="inline-flex items-center justify-center rounded-md border border-slate-300 px-3 py-2 text-xs font-medium text-slate-700 hover:bg-slate-50"
                onClick={() => void loadSettings()}
                disabled={loading}
              >
                {loading ? "Loading..." : "Reload settings"}
              </button>
            </div>
          </section>

          <section className="space-y-3">
            <div className="flex items-center justify-between">
              <div>
                <h2 className="text-sm font-medium text-slate-700">Mode</h2>
                <p className="text-xs text-slate-500">
                  Choose how clients authenticate. When set to{" "}
                  <span className="font-semibold">None</span>, the API is open.
                </p>
              </div>
              <span className="inline-flex items-center rounded-full bg-amber-50 px-2.5 py-0.5 text-xs font-medium text-amber-700 border border-amber-100">
                {form.mode === "None" ? "OPEN" : "PROTECTED"}
              </span>
            </div>
            <div className="grid grid-cols-1 sm:grid-cols-4 gap-3">
              {(["None", "Bearer", "CCAPIKey", "OAuth"] as AuthMode[]).map((mode) => (
                <button
                  key={mode}
                  type="button"
                  onClick={() => handleChange("mode", mode)}
                  className={`rounded-md border px-3 py-2 text-sm text-left ${
                    form.mode === mode
                      ? "border-sky-500 bg-sky-50 text-sky-900"
                      : "border-slate-300 bg-white text-slate-700"
                  }`}
                >
                  <div className="font-medium">{mode}</div>
                  <div className="text-xs text-slate-500">
                    {mode === "None" && "No authentication required."}
                    {mode === "Bearer" && "Shared secret in Authorization: Bearer header."}
                    {mode === "CCAPIKey" && "Shared secret in CCAPIKey header."}
                    {mode === "OAuth" && "Issue access and refresh tokens via Auth API."}
                  </div>
                </button>
              ))}
            </div>
          </section>

          {(form.mode === "Bearer" || form.mode === "CCAPIKey") && (
            <section className="space-y-3 border-t border-slate-100 pt-4">
              <div className="flex items-center justify-between">
                <div>
                  <h2 className="text-sm font-medium text-slate-700">
                    {form.mode === "CCAPIKey" ? "CCAPIKey shared secret" : "Bearer token"}
                  </h2>
                  <p className="text-xs text-slate-500">
                    {form.mode === "CCAPIKey" ? (
                      <>
                        Clients must send <code>CCAPIKey: &lt;secret&gt;</code> on each request.
                      </>
                    ) : (
                      <>
                        Clients must send <code>Authorization: Bearer &lt;token&gt;</code> on each
                        request.
                      </>
                    )}
                  </p>
                </div>
                <button
                  type="button"
                  className="text-xs text-sky-700 hover:text-sky-900"
                  onClick={handleGenerateBearer}
                >
                  Generate token
                </button>
              </div>
              <div className="flex flex-col gap-3">
                <div className="flex items-center gap-2">
                  <input
                    type={showSecrets ? "text" : "password"}
                    className="flex-1 rounded-md border border-slate-300 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-sky-500"
                    value={form.bearerToken}
                    onChange={(e) => handleChange("bearerToken", e.target.value)}
                    placeholder={form.mode === "CCAPIKey" ? "Shared CCAPIKey secret" : "Shared bearer token"}
                  />
                  <button
                    type="button"
                    className="text-xs text-slate-600 hover:text-slate-800"
                    onClick={() => setShowSecrets((v) => !v)}
                  >
                    {showSecrets ? "Hide" : "Show"}
                  </button>
                  <button
                    type="button"
                    className="text-xs text-sky-700 hover:text-sky-900"
                    onClick={() => copyToClipboard(form.bearerToken)}
                    disabled={!form.bearerToken}
                  >
                    Copy
                  </button>
                </div>
                <div className="text-xs text-slate-500">
                  Example header:{" "}
                  {form.mode === "CCAPIKey" ? (
                    <code>CCAPIKey: {mask(form.bearerToken || "your-secret")}</code>
                  ) : (
                    <code>Authorization: Bearer {mask(form.bearerToken || "your-token")}</code>
                  )}
                </div>
              </div>
            </section>
          )}

          {form.mode === "OAuth" && (
            <section className="space-y-4 border-t border-slate-100 pt-4">
              <div className="flex items-center justify-between">
                <div>
                  <h2 className="text-sm font-medium text-slate-700">Internal OAuth client</h2>
                  <p className="text-xs text-slate-500">
                    Backend issues opaque access and refresh tokens using simple client
                    credentials.
                  </p>
                </div>
                <button
                  type="button"
                  className="text-xs text-sky-700 hover:text-sky-900"
                  onClick={handleGenerateClient}
                >
                  Generate client
                </button>
              </div>

              <div className="grid grid-cols-1 md:grid-cols-2 gap-3">
                <div className="space-y-1">
                  <label className="block text-xs font-medium text-slate-700">Client ID</label>
                  <div className="flex items-center gap-2">
                    <input
                      type="text"
                      className="flex-1 rounded-md border border-slate-300 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-sky-500"
                      value={form.oAuthClientId}
                      onChange={(e) => handleChange("oAuthClientId", e.target.value)}
                      placeholder="client id"
                    />
                    <button
                      type="button"
                      className="text-xs text-sky-700 hover:text-sky-900"
                      onClick={() => copyToClipboard(form.oAuthClientId)}
                      disabled={!form.oAuthClientId}
                    >
                      Copy
                    </button>
                  </div>
                </div>

                <div className="space-y-1">
                  <label className="block text-xs font-medium text-slate-700">Client secret</label>
                  <div className="flex items-center gap-2">
                    <input
                      type={showSecrets ? "text" : "password"}
                      className="flex-1 rounded-md border border-slate-300 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-sky-500"
                      value={form.oAuthClientSecret}
                      onChange={(e) => handleChange("oAuthClientSecret", e.target.value)}
                      placeholder="client secret"
                    />
                    <button
                      type="button"
                      className="text-xs text-slate-600 hover:text-slate-800"
                      onClick={() => setShowSecrets((v) => !v)}
                    >
                      {showSecrets ? "Hide" : "Show"}
                    </button>
                    <button
                      type="button"
                      className="text-xs text-sky-700 hover:text-sky-900"
                      onClick={() => copyToClipboard(form.oAuthClientSecret)}
                      disabled={!form.oAuthClientSecret}
                    >
                      Copy
                    </button>
                  </div>
                </div>
              </div>

              <div className="grid grid-cols-1 md:grid-cols-2 gap-3">
                <div className="space-y-1">
                  <label className="block text-xs font-medium text-slate-700">
                    Access token lifetime (minutes)
                  </label>
                  <input
                    type="number"
                    min={1}
                    className="w-full rounded-md border border-slate-300 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-sky-500"
                    value={form.accessTokenLifetimeMinutes}
                    onChange={(e) => handleChange("accessTokenLifetimeMinutes", Number(e.target.value) || 1)}
                  />
                </div>
                <div className="space-y-1">
                  <label className="block text-xs font-medium text-slate-700">
                    Refresh token lifetime (days)
                  </label>
                  <input
                    type="number"
                    min={1}
                    className="w-full rounded-md border border-slate-300 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-sky-500"
                    value={form.refreshTokenLifetimeDays}
                    onChange={(e) => handleChange("refreshTokenLifetimeDays", Number(e.target.value) || 1)}
                  />
                </div>
              </div>

              <div className="space-y-2 text-xs text-slate-500 bg-slate-50 border border-slate-200 rounded-md px-3 py-2">
                <div className="font-medium text-slate-700">Token endpoints</div>
                <div>
                  <code>POST /api/v1/auth/token</code> with body:
                  <pre className="mt-1 rounded bg-slate-900 text-slate-50 p-2 text-[11px] overflow-x-auto">
{`{
  "clientId": "${form.oAuthClientId || "your-client-id"}",
  "clientSecret": "${mask(form.oAuthClientSecret || "your-client-secret")}"
}`}
                  </pre>
                </div>
                <div>
                  <code>POST /api/v1/auth/refresh</code> with body:
                  <pre className="mt-1 rounded bg-slate-900 text-slate-50 p-2 text-[11px] overflow-x-auto">
{`{
  "refreshToken": "<refresh_token>"
}`}
                  </pre>
                </div>
                <div>
                  Example usage:
                  <pre className="mt-1 rounded bg-slate-900 text-slate-50 p-2 text-[11px] overflow-x-auto">
{`curl -X POST "${import.meta.env.VITE_API_BASE_URL}/api/v1/auth/token" \\
  -H "Content-Type: application/json" \\
  -d '{"clientId":"${form.oAuthClientId || "your-client-id"}","clientSecret":"${form.oAuthClientSecret || "your-client-secret"}"}'`}
                  </pre>
                </div>
                {hasAnyTokens && (
                  <div className="text-amber-700">
                    Tokens have been issued. Switching modes will clear them on the server.
                  </div>
                )}
              </div>
            </section>
          )}

          <section className="space-y-2 border-t border-slate-100 pt-4">
            {error && <div className="text-sm text-red-600">{error}</div>}
            {success && <div className="text-sm text-emerald-700">{success}</div>}
            {form.mode === "None" && (
              <div className="text-xs text-amber-700 bg-amber-50 border border-amber-200 rounded-md px-3 py-2">
                The API is currently open. Any client can call endpoints without authentication.
              </div>
            )}

            <div className="flex justify-end gap-3">
              <button
                type="submit"
                className="inline-flex items-center justify-center rounded-md bg-sky-600 px-4 py-2 text-sm font-medium text-white shadow-sm hover:bg-sky-700 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-sky-500"
                disabled={saving}
              >
                {saving ? "Saving..." : "Save settings"}
              </button>
            </div>
          </section>
        </form>
    </div>
  );
};

export default AuthSettingsPage;

