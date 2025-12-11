import { AlertTriangle, Search, BarChart, CheckCircle } from 'lucide-react';

export const SafetyRiskManagement = () => {
  const processes = [
    "Use proactive, reactive, and predictive methods to identify hazards",
    "Analyze risk based on standardized severity and likelihood classifications",
    "Determine and implement appropriate mitigation strategies aligned with acceptable risk levels",
    "Verify, monitor, and document the effectiveness of implemented controls",
    "Maintain a centralized hazard reporting and tracking system",
    "Conduct risk assessments for operational changes, new technologies, and procedures",
    "Ensure all risk assessments are recorded, traceable, and subject to periodic review"
  ];

  const hazardSources = [
    "Operational and maintenance safety reports",
    "Audits, inspections, and investigations",
    "Weather, environment, and airport conditions",
    "Equipment and system performance data",
    "Feedback from employees, contractors, and stakeholders",
    "Voluntary and mandatory reporting systems"
  ];

  return (
    <section id="srm" className="py-24 bg-muted/30">
      <div className="container mx-auto px-4">
        <div className="max-w-4xl mx-auto">
          <div className="flex items-center gap-3 mb-6">
            <div className="p-3 bg-gold/10 rounded-xl">
              <AlertTriangle className="h-8 w-8 text-gold" />
            </div>
            <h2 className="text-4xl font-bold text-foreground">Safety Risk Management</h2>
          </div>

          <div className="bg-card rounded-2xl shadow-xl border border-border overflow-hidden">
            <div className="bg-gradient-to-r from-gold/80 to-gold p-6">
              <h3 className="text-xl font-bold text-navy">SRM Process</h3>
              <p className="text-navy/70 text-sm">Integrated Risk Management Framework</p>
            </div>

            <div className="p-8">
              <p className="text-foreground/80 leading-relaxed mb-8 text-lg">
                STAR AIR Aviation integrates Safety Risk Management (SRM) into all operational, maintenance, administrative, and organizational activities.
              </p>

              <div className="grid md:grid-cols-2 gap-8 mb-8">
                <div>
                  <h4 className="text-lg font-semibold text-foreground mb-4 flex items-center gap-2">
                    <BarChart className="h-5 w-5 text-gold" />
                    We will:
                  </h4>
                  <ul className="space-y-3">
                    {processes.map((process, index) => (
                      <li key={index} className="flex items-start gap-3">
                        <CheckCircle className="h-5 w-5 text-primary flex-shrink-0 mt-0.5" />
                        <span className="text-foreground/80 text-sm">{process}</span>
                      </li>
                    ))}
                  </ul>
                </div>

                <div>
                  <h4 className="text-lg font-semibold text-foreground mb-4 flex items-center gap-2">
                    <Search className="h-5 w-5 text-gold" />
                    Hazard Identification Sources:
                  </h4>
                  <ul className="space-y-3">
                    {hazardSources.map((source, index) => (
                      <li key={index} className="flex items-start gap-3">
                        <span className="flex-shrink-0 w-2 h-2 bg-gold rounded-full mt-2" />
                        <span className="text-foreground/80 text-sm">{source}</span>
                      </li>
                    ))}
                  </ul>
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>
    </section>
  );
};
