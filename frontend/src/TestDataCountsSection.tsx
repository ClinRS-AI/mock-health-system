import React, { useEffect, useState } from "react";
import {
  PieChart,
  Pie,
  Cell,
  ResponsiveContainer,
  Legend,
  Tooltip,
  BarChart,
  Bar,
  XAxis,
  YAxis,
  CartesianGrid
} from "recharts";
import {
  getPatientTestDataStats,
  getStudyTestDataStats,
  getSubjectTestDataStats,
  type PatientTestDataStats,
  type StudyTestDataStats,
  type SubjectTestDataStats,
  type SubjectStudyStatusBreakdown
} from "./api";
import { useAdminSession } from "./AdminSessionContext";
import { DEMO_TEST_DATA_STATS, DEMO_STUDY_TEST_DATA_STATS, DEMO_SUBJECT_TEST_DATA_STATS } from "./demoData";

const CHART_COLORS = ["#0284c7", "#059669", "#d97706", "#7c3aed", "#db2777", "#0891b2", "#65a30d", "#dc2626"];

function chartColor(index: number): string {
  return CHART_COLORS[index % CHART_COLORS.length];
}

interface CategoryPieChartProps {
  data: { name: string; value: number }[];
  valueLabel: string;
  emptyLabel: string;
}

function CategoryPieChart({ data, valueLabel, emptyLabel }: CategoryPieChartProps) {
  if (data.length === 0) {
    return <p className="text-xs text-slate-400">{emptyLabel}</p>;
  }
  return (
    <ResponsiveContainer width="100%" height={220}>
      <PieChart margin={{ top: 10, right: 10, bottom: 10, left: 10 }}>
        <Pie data={data} cx="50%" cy="50%" innerRadius={45} outerRadius={65} paddingAngle={2} dataKey="value">
          {data.map((d, index) => (
            <Cell key={d.name} fill={chartColor(index)} />
          ))}
        </Pie>
        <Tooltip formatter={(value: number) => [value, valueLabel]} />
        <Legend />
      </PieChart>
    </ResponsiveContainer>
  );
}

// CC's fixed 9-value Subject status vocabulary (research.md Decision 11), ordered to match the
// typical chronological progression of a subject through a study (not the Active/Inactive
// category grouping used elsewhere) so stacked-bar segments read left-to-right as a timeline;
// any unrecognized status still renders (appended after these), it just won't have a fixed
// position.
const KNOWN_SUBJECT_STATUSES = [
  "Prescreened",
  "Non Qualified",
  "Screened",
  "Screen Failed",
  "Run-in",
  "Run-in Failed",
  "Randomized",
  "Dropped",
  "Complete"
];

interface SubjectStatusStackedBarChartProps {
  data: SubjectStudyStatusBreakdown[];
  emptyLabel: string;
}

function SubjectStatusStackedBarChart({ data, emptyLabel }: SubjectStatusStackedBarChartProps) {
  if (data.length === 0) {
    return <p className="text-xs text-slate-400">{emptyLabel}</p>;
  }

  const statusesPresent = Array.from(new Set(data.flatMap((study) => study.byStatus.map((s) => s.statusName))));
  const orderedKnown = KNOWN_SUBJECT_STATUSES.filter((s) => statusesPresent.includes(s));
  const unknownStatuses = statusesPresent.filter((s) => !KNOWN_SUBJECT_STATUSES.includes(s));
  const statuses = [...orderedKnown, ...unknownStatuses];

  const chartData = data.map((study) => {
    const row: Record<string, string | number> = { studyName: study.studyName };
    for (const status of statuses) {
      row[status] = study.byStatus.find((s) => s.statusName === status)?.count ?? 0;
    }
    return row;
  });

  // Recharts' Tooltip and Legend both default to alphabetically sorting their items
  // (itemSorter: 'name' / 'value' respectively) regardless of Bar declaration order, so both
  // need an explicit itemSorter tied back to `statuses` to keep the chronological order.
  const byChronologicalStatus = (item: { dataKey?: unknown }) => statuses.indexOf(String(item.dataKey));

  // Horizontal layout: study names run down the Y-axis as row labels (full text, no truncation
  // or rotation needed) with bars extending rightward, stacked by status.
  return (
    <ResponsiveContainer width="100%" height={Math.max(220, data.length * 40)}>
      <BarChart data={chartData} layout="vertical" margin={{ top: 10, right: 10, bottom: 10, left: 10 }}>
        <CartesianGrid strokeDasharray="3 3" horizontal={false} />
        <XAxis type="number" allowDecimals={false} tick={{ fontSize: 10 }} />
        <YAxis type="category" dataKey="studyName" tick={{ fontSize: 10 }} width={180} />
        <Tooltip wrapperStyle={{ zIndex: 10 }} itemSorter={byChronologicalStatus} />
        <Legend wrapperStyle={{ fontSize: 11 }} itemSorter={byChronologicalStatus} />
        {statuses.map((status, index) => (
          <Bar key={status} dataKey={status} stackId="status" fill={chartColor(index)} />
        ))}
      </BarChart>
    </ResponsiveContainer>
  );
}

const TestDataCountsSection: React.FC = () => {
  const { hasSession, isDemoMode, isProbeSettled } = useAdminSession();

  const [stats, setStats] = useState<PatientTestDataStats | null>(null);
  const [studiesStats, setStudiesStats] = useState<StudyTestDataStats | null>(null);
  const [subjectsStats, setSubjectsStats] = useState<SubjectTestDataStats | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [loadingStats, setLoadingStats] = useState(false);

  async function loadStats() {
    try {
      const data = await getPatientTestDataStats();
      setStats(data);
    } catch (err) {
      console.error(err);
      setStats(null);
      setError("Unable to load patient stats. Check the admin key and backend.");
    }
  }

  async function loadStudiesStats() {
    try {
      const data = await getStudyTestDataStats();
      setStudiesStats(data);
    } catch (err) {
      console.error(err);
      setStudiesStats(null);
      setError("Unable to load study stats. Check the admin key and backend.");
    }
  }

  async function loadSubjectsStats() {
    try {
      const data = await getSubjectTestDataStats();
      setSubjectsStats(data);
    } catch (err) {
      console.error(err);
      setSubjectsStats(null);
      setError("Unable to load subject stats. Check the admin key and backend.");
    }
  }

  async function refreshStats() {
    if (isDemoMode) return;
    setLoadingStats(true);
    setError(null);
    await Promise.all([loadStats(), loadStudiesStats(), loadSubjectsStats()]);
    setLoadingStats(false);
  }

  useEffect(() => {
    if (!hasSession && !isProbeSettled) return;
    if (isDemoMode) {
      setStats(DEMO_TEST_DATA_STATS);
      setStudiesStats(DEMO_STUDY_TEST_DATA_STATS);
      setSubjectsStats(DEMO_SUBJECT_TEST_DATA_STATS);
    } else {
      setError(null);
      void loadStats();
      void loadStudiesStats();
      void loadSubjectsStats();
    }
  }, [hasSession, isDemoMode, isProbeSettled]);

  return (
    <div className="space-y-4">
      <section className="space-y-3">
        <div className="flex flex-col sm:flex-row sm:items-start sm:justify-between gap-3">
          <div>
            <h2 className="text-sm font-medium text-slate-700">Data Counts and Visualizations</h2>
            <p className="text-xs text-slate-500">
              Current volume of synthetic patient and study data in this environment.
            </p>
          </div>
          <button
            type="button"
            onClick={() => void refreshStats()}
            disabled={loadingStats}
            className="inline-flex items-center justify-center rounded-md border border-slate-300 px-3 py-2 text-xs font-medium text-slate-700 hover:bg-slate-50 self-start disabled:opacity-70"
          >
            {loadingStats ? "Refreshing…" : "Refresh stats"}
          </button>
        </div>
      </section>

      {error && <div className="text-sm text-red-600">{error}</div>}

      {stats && (
        <section className="grid gap-4 md:grid-cols-4">
          <div className="rounded-lg border border-slate-200 bg-white p-4">
            <p className="text-xs font-medium text-slate-500">Patient count</p>
            <p className="mt-1 text-xl font-semibold text-slate-800 tabular-nums">
              {stats.patientCount}
            </p>
          </div>
          <div className="rounded-lg border border-slate-200 bg-white p-4">
            <p className="text-xs font-medium text-slate-500">Duplicate patients</p>
            <p className="mt-1 text-xl font-semibold text-slate-800 tabular-nums">
              {stats.duplicatePatientCount}
            </p>
          </div>
          <div className="rounded-lg border border-slate-200 bg-white p-4">
            <p className="text-xs font-medium text-slate-500">Recent audit events (last 5 min)</p>
            <p className="mt-1 text-xl font-semibold text-slate-800 tabular-nums">
              {stats.recentAuditEventCount}
            </p>
          </div>
          <div className="rounded-lg border border-slate-200 bg-white p-4">
            <p className="text-xs font-medium text-slate-500">Total staff</p>
            <p className="mt-1 text-xl font-semibold text-slate-800 tabular-nums">
              {stats.totalStaffCount}
            </p>
          </div>
        </section>
      )}

      {stats && (
        <section className="rounded-lg border border-slate-200 bg-white p-4">
          <p className="mb-2 text-xs font-medium text-slate-500">Patients by site</p>
          <CategoryPieChart
            data={stats.patientsBySite.map((s) => ({ name: s.siteName, value: s.count }))}
            valueLabel="Patients"
            emptyLabel="No patients yet."
          />
        </section>
      )}

      {studiesStats && (
        <section className="grid gap-4 md:grid-cols-3">
          <div className="rounded-lg border border-slate-200 bg-white p-4">
            <p className="text-xs font-medium text-slate-500">Study count</p>
            <p className="mt-1 text-xl font-semibold text-slate-800 tabular-nums">
              {studiesStats.studyCount}
            </p>
          </div>
          <div className="rounded-lg border border-slate-200 bg-white p-4">
            <p className="text-xs font-medium text-slate-500">Arms</p>
            <p className="mt-1 text-xl font-semibold text-slate-800 tabular-nums">
              {studiesStats.armCount}
            </p>
          </div>
          <div className="rounded-lg border border-slate-200 bg-white p-4">
            <p className="text-xs font-medium text-slate-500">Visits</p>
            <p className="mt-1 text-xl font-semibold text-slate-800 tabular-nums">
              {studiesStats.visitCount}
            </p>
          </div>
        </section>
      )}

      {studiesStats && (
        <section className="rounded-lg border border-slate-200 bg-white p-4">
          <p className="mb-2 text-xs font-medium text-slate-500">Studies by status</p>
          <CategoryPieChart
            data={studiesStats.studiesByStatus.map((s) => ({ name: s.statusName, value: s.count }))}
            valueLabel="Studies"
            emptyLabel="No studies yet."
          />
        </section>
      )}

      {subjectsStats && (
        <section className="grid gap-4 md:grid-cols-4">
          <div className="rounded-lg border border-slate-200 bg-white p-4">
            <p className="text-xs font-medium text-slate-500">Subject count</p>
            <p className="mt-1 text-xl font-semibold text-slate-800 tabular-nums">
              {subjectsStats.subjectCount}
            </p>
          </div>
        </section>
      )}

      {subjectsStats && (
        <section className="rounded-lg border border-slate-200 bg-white p-4">
          <p className="mb-2 text-xs font-medium text-slate-500">
            Subjects by study and status (top 10 studies by volume)
          </p>
          <SubjectStatusStackedBarChart
            data={subjectsStats.topStudiesBySubjectStatus}
            emptyLabel="No subjects yet."
          />
        </section>
      )}
    </div>
  );
};

export default TestDataCountsSection;
