import { useState } from 'react';
import { Table, Filter, Download } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { generateRiskMatrixPDF } from '@/lib/pdfGenerator';

type RiskLevel = 'All' | 'High' | 'Medium' | 'Low';

interface RiskItem {
  id: number;
  hazard: string;
  consequence: string;
  severity: number;
  likelihood: number;
  riskLevel: 'High' | 'Medium' | 'Low';
  mitigation: string;
  residualSeverity: number;
  residualLikelihood: number;
  residualRisk: 'High' | 'Medium' | 'Low';
}

const riskData: RiskItem[] = [
  {
    id: 1,
    hazard: "Bird strike on takeoff",
    consequence: "Engine damage / rejected takeoff",
    severity: 4,
    likelihood: 4,
    riskLevel: "High",
    mitigation: "Wildlife control, pilot briefing, radar monitoring",
    residualSeverity: 3,
    residualLikelihood: 2,
    residualRisk: "Medium"
  },
  {
    id: 2,
    hazard: "Runway incursion",
    consequence: "Collision with aircraft/vehicle",
    severity: 5,
    likelihood: 3,
    riskLevel: "High",
    mitigation: "Enhanced signage, surface radar, ATC coordination",
    residualSeverity: 4,
    residualLikelihood: 1,
    residualRisk: "Low"
  },
  {
    id: 3,
    hazard: "Fatigue among flight crew",
    consequence: "Reduced situational awareness",
    severity: 4,
    likelihood: 3,
    riskLevel: "High",
    mitigation: "Duty-time limits, fatigue reporting, roster review",
    residualSeverity: 3,
    residualLikelihood: 2,
    residualRisk: "Medium"
  },
  {
    id: 4,
    hazard: "Weather-related low visibility",
    consequence: "Hard landing / unstable approach",
    severity: 4,
    likelihood: 4,
    riskLevel: "High",
    mitigation: "Autoland systems, stricter minima, go-around policy",
    residualSeverity: 3,
    residualLikelihood: 2,
    residualRisk: "Medium"
  },
  {
    id: 5,
    hazard: "Miscommunication between ATC and cockpit",
    consequence: "Loss of separation",
    severity: 4,
    likelihood: 3,
    riskLevel: "High",
    mitigation: "Standard phraseology, radio checks, CRM training",
    residualSeverity: 3,
    residualLikelihood: 2,
    residualRisk: "Medium"
  },
  {
    id: 6,
    hazard: "Maintenance error",
    consequence: "System failure in flight",
    severity: 5,
    likelihood: 3,
    riskLevel: "High",
    mitigation: "Dual inspections, digital checklists, staff training",
    residualSeverity: 4,
    residualLikelihood: 2,
    residualRisk: "Medium"
  },
  {
    id: 7,
    hazard: "Fuel miscalculation",
    consequence: "In-flight fuel emergency",
    severity: 5,
    likelihood: 3,
    riskLevel: "High",
    mitigation: "Cross-checking, automated calculations, dispatch checks",
    residualSeverity: 4,
    residualLikelihood: 2,
    residualRisk: "Medium"
  },
  {
    id: 8,
    hazard: "Cabin crew insufficient training",
    consequence: "Poor emergency response",
    severity: 4,
    likelihood: 3,
    riskLevel: "High",
    mitigation: "Recurrent training, standardized procedures",
    residualSeverity: 3,
    residualLikelihood: 2,
    residualRisk: "Medium"
  },
  {
    id: 9,
    hazard: "Ground handling congestion",
    consequence: "Vehicle collision / delay",
    severity: 3,
    likelihood: 3,
    riskLevel: "Medium",
    mitigation: "Marked zones, staff coordination, slow-speed rules",
    residualSeverity: 3,
    residualLikelihood: 1,
    residualRisk: "Low"
  }
];

const getRiskBadgeColor = (level: string) => {
  switch (level) {
    case 'High':
      return 'bg-risk-high/10 text-risk-high border-risk-high/30';
    case 'Medium':
      return 'bg-risk-medium/10 text-risk-medium border-risk-medium/30';
    case 'Low':
      return 'bg-risk-low/10 text-risk-low border-risk-low/30';
    default:
      return 'bg-muted text-muted-foreground';
  }
};

export const RiskMatrix = () => {
  const [filter, setFilter] = useState<RiskLevel>('All');

  const filteredData = filter === 'All' 
    ? riskData 
    : riskData.filter(item => item.riskLevel === filter);

  const handleDownloadMatrix = () => {
    generateRiskMatrixPDF(filteredData);
  };

  return (
    <section id="matrix" className="py-24 bg-background">
      <div className="container mx-auto px-4">
        <div className="max-w-6xl mx-auto">
          <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4 mb-6">
            <div className="flex items-center gap-3">
              <div className="p-3 bg-destructive/10 rounded-xl">
                <Table className="h-8 w-8 text-destructive" />
              </div>
              <h2 className="text-4xl font-bold text-foreground">SRM Risk Matrix</h2>
            </div>
            <Button onClick={handleDownloadMatrix} className="bg-primary hover:bg-primary/90 gap-2">
              <Download className="h-4 w-4" />
              Download Matrix
            </Button>
          </div>

          {/* Filter Buttons */}
          <div className="flex items-center gap-2 mb-6 flex-wrap">
            <div className="flex items-center gap-2 text-muted-foreground mr-2">
              <Filter className="h-4 w-4" />
              <span className="text-sm font-medium">Filter:</span>
            </div>
            {(['All', 'High', 'Medium', 'Low'] as RiskLevel[]).map((level) => (
              <button
                key={level}
                onClick={() => setFilter(level)}
                className={`px-4 py-2 rounded-lg text-sm font-medium transition-all ${
                  filter === level
                    ? level === 'All'
                      ? 'bg-foreground text-background'
                      : level === 'High'
                        ? 'bg-risk-high text-white'
                        : level === 'Medium'
                          ? 'bg-risk-medium text-white'
                          : 'bg-risk-low text-white'
                    : 'bg-muted text-muted-foreground hover:bg-muted/80'
                }`}
              >
                {level}
              </button>
            ))}
          </div>

          {/* Table Container with Horizontal Scroll */}
          <div className="bg-card rounded-2xl shadow-xl border border-border overflow-hidden">
            <div className="overflow-x-auto">
              <table className="w-full min-w-[1200px]">
                <thead>
                  <tr className="bg-navy text-primary-foreground">
                    <th className="px-4 py-4 text-left text-sm font-semibold">ID</th>
                    <th className="px-4 py-4 text-left text-sm font-semibold">Hazard</th>
                    <th className="px-4 py-4 text-left text-sm font-semibold">Consequence</th>
                    <th className="px-4 py-4 text-center text-sm font-semibold">Sev.</th>
                    <th className="px-4 py-4 text-center text-sm font-semibold">Lik.</th>
                    <th className="px-4 py-4 text-center text-sm font-semibold">Risk Level</th>
                    <th className="px-4 py-4 text-left text-sm font-semibold">Mitigation</th>
                    <th className="px-4 py-4 text-center text-sm font-semibold">Res. Sev.</th>
                    <th className="px-4 py-4 text-center text-sm font-semibold">Res. Lik.</th>
                    <th className="px-4 py-4 text-center text-sm font-semibold">Res. Risk</th>
                  </tr>
                </thead>
                <tbody>
                  {filteredData.map((item, index) => (
                    <tr
                      key={item.id}
                      className={`border-b border-border transition-colors hover:bg-muted/50 ${
                        index % 2 === 0 ? 'bg-background' : 'bg-muted/20'
                      }`}
                    >
                      <td className="px-4 py-4 text-sm font-semibold text-foreground">{item.id}</td>
                      <td className="px-4 py-4 text-sm text-foreground font-medium">{item.hazard}</td>
                      <td className="px-4 py-4 text-sm text-muted-foreground">{item.consequence}</td>
                      <td className="px-4 py-4 text-center text-sm font-semibold text-foreground">{item.severity}</td>
                      <td className="px-4 py-4 text-center text-sm font-semibold text-foreground">{item.likelihood}</td>
                      <td className="px-4 py-4 text-center">
                        <span className={`px-3 py-1 rounded-full text-xs font-semibold border ${getRiskBadgeColor(item.riskLevel)}`}>
                          {item.riskLevel}
                        </span>
                      </td>
                      <td className="px-4 py-4 text-sm text-muted-foreground max-w-xs">{item.mitigation}</td>
                      <td className="px-4 py-4 text-center text-sm font-semibold text-foreground">{item.residualSeverity}</td>
                      <td className="px-4 py-4 text-center text-sm font-semibold text-foreground">{item.residualLikelihood}</td>
                      <td className="px-4 py-4 text-center">
                        <span className={`px-3 py-1 rounded-full text-xs font-semibold border ${getRiskBadgeColor(item.residualRisk)}`}>
                          {item.residualRisk}
                        </span>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          </div>

          {/* Legend */}
          <div className="mt-6 flex flex-wrap items-center gap-6 text-sm text-muted-foreground">
            <span className="font-medium">Risk Level Legend:</span>
            <div className="flex items-center gap-2">
              <span className="w-3 h-3 rounded-full bg-risk-high" />
              <span>High (12-25)</span>
            </div>
            <div className="flex items-center gap-2">
              <span className="w-3 h-3 rounded-full bg-risk-medium" />
              <span>Medium (5-11)</span>
            </div>
            <div className="flex items-center gap-2">
              <span className="w-3 h-3 rounded-full bg-risk-low" />
              <span>Low (1-4)</span>
            </div>
          </div>
        </div>
      </div>
    </section>
  );
};
