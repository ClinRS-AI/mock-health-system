import React, { useState } from "react";
import AdminAccessPage from "./AdminAccessPage";
import AdminSessionProvider from "./AdminSessionContext";
import AuthSettingsPage from "./AuthSettingsPage";
import DemoBanner from "./DemoBanner";
import MonitoringPage from "./MonitoringPage";
import TestDataPage from "./TestDataPage";
import { getApiStatus } from "./api";
import { useAdminSession } from "./AdminSessionContext";

type AppView = "status" | "admin" | "auth" | "monitoring" | "testData";

const DEMO_BANNER_VIEWS: AppView[] = ["auth", "monitoring", "testData"];

function AppContent() {
  const [view, setView] = useState<AppView>("status");
  const [status, setStatus] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const { isDemoMode } = useAdminSession();

  async function handleCheckStatus() {
    try {
      setLoading(true);
      setError(null);
      const result = await getApiStatus();
      setStatus(result);
    } catch (err) {
      console.error(err);
      setError("Unable to reach the API. Is the backend running?");
      setStatus(null);
    } finally {
      setLoading(false);
    }
  }

  const showDemoBanner = isDemoMode && DEMO_BANNER_VIEWS.includes(view);

  return (
    <div className="min-h-screen flex items-center justify-center px-4">
      <div className="w-full max-w-4xl bg-white shadow-lg rounded-xl p-6 space-y-6">
        <header className="space-y-1">
          <h1 className="text-2xl font-semibold tracking-tight text-slate-900">
            Mock Health System
          </h1>
          <p className="text-sm text-slate-500">
            Fake health system for development and testing. Mocks API endpoints for CC CTMS based off the public API documentation.
          </p>
        </header>

        <nav className="flex flex-wrap gap-3 border-b border-slate-100 pb-3 text-sm">
          <button
            type="button"
            className={`px-3 py-1.5 rounded-md ${
              view === "status"
                ? "bg-sky-50 text-sky-700 border border-sky-200"
                : "text-slate-600 hover:bg-slate-50 border border-transparent"
            }`}
            onClick={() => setView("status")}
          >
            API status
          </button>
          <button
            type="button"
            className={`px-3 py-1.5 rounded-md ${
              view === "admin"
                ? "bg-sky-50 text-sky-700 border border-sky-200"
                : "text-slate-600 hover:bg-slate-50 border border-transparent"
            }`}
            onClick={() => setView("admin")}
          >
            Admin access
          </button>
          <button
            type="button"
            className={`px-3 py-1.5 rounded-md ${
              view === "auth"
                ? "bg-sky-50 text-sky-700 border border-sky-200"
                : "text-slate-600 hover:bg-slate-50 border border-transparent"
            }`}
            onClick={() => setView("auth")}
          >
            Authentication settings
          </button>
          <button
            type="button"
            className={`px-3 py-1.5 rounded-md ${
              view === "monitoring"
                ? "bg-sky-50 text-sky-700 border border-sky-200"
                : "text-slate-600 hover:bg-slate-50 border border-transparent"
            }`}
            onClick={() => setView("monitoring")}
          >
            Monitoring
          </button>
          <button
            type="button"
            className={`px-3 py-1.5 rounded-md ${
              view === "testData"
                ? "bg-sky-50 text-sky-700 border border-sky-200"
                : "text-slate-600 hover:bg-slate-50 border border-transparent"
            }`}
            onClick={() => setView("testData")}
          >
            Test data management
          </button>
        </nav>

        <main className="space-y-4">
          {showDemoBanner && (
            <DemoBanner onNavigateToAdmin={() => setView("admin")} />
          )}

          {view === "status" && (
            <>
            <section className="space-y-2">
              <h2 className="text-sm font-medium text-slate-700">Stack overview</h2>
              <ul className="text-sm text-slate-600 list-disc list-inside space-y-1">
                <li>.NET 10 Web API backend (minimal hosting model).</li>
                <li>Postgres-ready data access via Entity Framework Core.</li>
                <li>React + Vite + Tailwind CSS frontend.</li>
                <li>Configuration via environment variables and `.env` files.</li>
              </ul>
            </section>

            <section className="space-y-2">
              <h2 className="text-sm font-medium text-slate-700">API status</h2>
              <button
                type="button"
                onClick={() => void handleCheckStatus()}
                className="inline-flex items-center justify-center rounded-lg bg-sky-600 px-4 py-2 text-sm font-medium text-white shadow-sm hover:bg-sky-700 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-sky-500"
                disabled={loading}
              >
                {loading ? "Checking..." : "Check API status"}
              </button>
              {error && <div className="text-sm text-red-600 mt-2">{error}</div>}
              {status && !error && (
                <div className="mt-2 rounded-lg border border-emerald-200 bg-emerald-50 px-3 py-2 text-sm text-emerald-800">
                  {status}
                </div>
              )}
            </section>
            </>
          )}

          {view === "admin" && <AdminAccessPage />}

          {view === "auth" && <AuthSettingsPage />}

          {view === "monitoring" && <MonitoringPage />}

          {view === "testData" && <TestDataPage />}
        </main>

        <footer className="pt-2 border-t border-slate-100 text-xs text-slate-400 flex justify-between">
          <span>Mock Health System</span>
          <span>Authentication-ready</span>
        </footer>
      </div>
    </div>
  );
}

function App() {
  return (
    <AdminSessionProvider>
      <AppContent />
    </AdminSessionProvider>
  );
}

export default App;
