import { ClipboardCheck, TrendingUp, Eye, Settings } from 'lucide-react';

export const SafetyAssurance = () => {
  const assuranceActivities = [
    { icon: TrendingUp, text: "Monitoring key safety performance indicators (SPIs) and trends" },
    { icon: ClipboardCheck, text: "Conducting scheduled and unscheduled internal audits" },
    { icon: Eye, text: "Verifying regulatory compliance and operational standards" },
    { icon: Settings, text: "Identifying root causes of safety deficiencies" },
  ];

  const additionalActivities = [
    "Validating the effectiveness of risk mitigations through follow-up actions",
    "Performing change-management safety reviews",
    "Analyzing data from reports, safety dashboards, and investigations",
    "Regularly reporting safety performance to senior leadership",
    "Ensuring continuous improvement of SMS processes"
  ];

  return (
    <section id="assurance" className="py-24 bg-background">
      <div className="container mx-auto px-4">
        <div className="max-w-4xl mx-auto">
          <div className="flex items-center gap-3 mb-6">
            <div className="p-3 bg-sky-blue/10 rounded-xl">
              <ClipboardCheck className="h-8 w-8 text-sky-blue" />
            </div>
            <h2 className="text-4xl font-bold text-foreground">Safety Assurance</h2>
          </div>

          <div className="bg-card rounded-2xl shadow-xl border border-border overflow-hidden">
            <div className="bg-gradient-to-r from-sky-blue/80 to-sky-blue p-6">
              <h3 className="text-xl font-bold text-navy">Assurance Framework</h3>
              <p className="text-navy/70 text-sm">Continuous Monitoring & Improvement</p>
            </div>

            <div className="p-8">
              <p className="text-foreground/80 leading-relaxed mb-8 text-lg">
                Safety Assurance ensures that safety controls remain effective and the SMS functions as intended.
              </p>

              <h4 className="text-lg font-semibold text-foreground mb-6">We achieve this by:</h4>

              <div className="grid sm:grid-cols-2 gap-4 mb-8">
                {assuranceActivities.map((activity, index) => (
                  <div key={index} className="bg-muted/50 rounded-xl p-4 flex items-start gap-4 hover:bg-muted transition-colors">
                    <div className="p-2 bg-sky-blue/10 rounded-lg">
                      <activity.icon className="h-5 w-5 text-sky-blue" />
                    </div>
                    <span className="text-foreground/80 text-sm">{activity.text}</span>
                  </div>
                ))}
              </div>

              <ul className="space-y-3">
                {additionalActivities.map((activity, index) => (
                  <li key={index} className="flex items-start gap-3">
                    <span className="flex-shrink-0 w-2 h-2 bg-sky-blue rounded-full mt-2" />
                    <span className="text-foreground/80">{activity}</span>
                  </li>
                ))}
              </ul>
            </div>
          </div>
        </div>
      </div>
    </section>
  );
};
