import React from "react";
import { useAdminSession } from "./AdminSessionContext";

/** Compact notice for admin-gated pages about current session state. */
const AdminSessionBanner: React.FC = () => {
  const { hasSession, expiresAtUtc, signOut } = useAdminSession();

  if (hasSession && expiresAtUtc) {
    const when = new Date(expiresAtUtc).toLocaleString(undefined, {
      dateStyle: "short",
      timeStyle: "short"
    });
    return (
      <div className="flex flex-wrap items-center justify-between gap-2 rounded-md border border-emerald-200 bg-emerald-50 px-3 py-2 text-xs text-emerald-900">
        <span>
          Admin session active until <span className="font-medium">{when}</span>.
        </span>
        <button
          type="button"
          onClick={() => signOut()}
          className="rounded border border-emerald-300 bg-white px-2 py-0.5 font-medium hover:bg-emerald-100"
        >
          Sign out
        </button>
      </div>
    );
  }

  return (
    <div className="rounded-md border border-amber-200 bg-amber-50 px-3 py-2 text-xs text-amber-900">
      No admin session in this tab. Open <strong>Admin access</strong> and enter the admin key to
      use Authentication settings, Monitoring, and Test data management when the server requires
      it.
    </div>
  );
};

export default AdminSessionBanner;
