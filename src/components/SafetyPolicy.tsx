import { FileCheck, Shield, Users } from 'lucide-react';

export const SafetyPolicy = () => {
  const commitments = [
    "Comply with all aviation safety laws, regulations, and standards",
    "Proactively identify hazards and manage associated risks",
    "Maintain an effective risk-based decision-making process in all activities",
    "Promote an open, Just Culture that encourages voluntary reporting without fear",
    "Allocate adequate resources, training, and technology to support safety",
    "Ensure timely communication of safety information across all departments",
    "Protect safety data and safeguard confidentiality of reporting sources",
    "Continuously measure, review, and improve safety performance"
  ];

  return (
    <section id="policy" className="py-24 bg-background">
      <div className="container mx-auto px-4">
        <div className="max-w-4xl mx-auto">
          <div className="flex items-center gap-3 mb-6">
            <div className="p-3 bg-primary/10 rounded-xl">
              <FileCheck className="h-8 w-8 text-primary" />
            </div>
            <h2 className="text-4xl font-bold text-foreground">Safety Policy</h2>
          </div>

          <div className="bg-card rounded-2xl shadow-xl border border-border overflow-hidden">
            <div className="bg-gradient-to-r from-navy to-navy-light p-6">
              <h3 className="text-xl font-bold text-primary-foreground">STAR AIR Aviation</h3>
              <p className="text-primary-foreground/70 text-sm">Safety Management System Policy Statement</p>
            </div>

            <div className="p-8">
              <p className="text-foreground/80 leading-relaxed mb-8 text-lg">
                Safety is the foundation of all our operations. STAR AIR Aviation is fully committed to achieving the highest levels of safety performance through the implementation and continuous enhancement of a comprehensive Safety Management System (SMS) in accordance with <span className="font-semibold text-primary">ICAO Annex 19</span> and all applicable national regulatory requirements.
              </p>

              <h4 className="text-lg font-semibold text-foreground mb-4 flex items-center gap-2">
                <Shield className="h-5 w-5 text-primary" />
                To fulfil this commitment, STAR AIR Aviation will:
              </h4>

              <ul className="space-y-3 mb-8">
                {commitments.map((commitment, index) => (
                  <li key={index} className="flex items-start gap-3">
                    <span className="flex-shrink-0 w-6 h-6 bg-primary/10 text-primary rounded-full flex items-center justify-center text-sm font-semibold">
                      {index + 1}
                    </span>
                    <span className="text-foreground/80">{commitment}</span>
                  </li>
                ))}
              </ul>

              <div className="bg-muted/50 rounded-xl p-6 border-l-4 border-primary">
                <div className="flex items-center gap-2 mb-3">
                  <Users className="h-5 w-5 text-primary" />
                  <span className="font-semibold text-foreground">Shared Responsibility</span>
                </div>
                <p className="text-foreground/70">
                  All employees, contractors, and partners share responsibility for safety and have full authority to stop or report any activity that may compromise safety.
                </p>
              </div>

              <div className="mt-8 pt-8 border-t border-border">
                <div className="flex flex-col sm:flex-row sm:items-end justify-between gap-4">
                  <div>
                    <p className="text-foreground font-semibold">Mohamed Abdelstar Abdelkader</p>
                    <p className="text-muted-foreground text-sm">Accountable Manager</p>
                  </div>
                  <p className="text-muted-foreground text-sm">Date: 30/12/2025</p>
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>
    </section>
  );
};
