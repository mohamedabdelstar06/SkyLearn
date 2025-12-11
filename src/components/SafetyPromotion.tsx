import { Megaphone, GraduationCap, MessageSquare, Heart } from 'lucide-react';

export const SafetyPromotion = () => {
  const sections = [
    {
      icon: GraduationCap,
      title: "Training",
      color: "primary",
      items: [
        "Provide SMS training tailored to each employee's role",
        "Conduct recurrent safety and refresher training programs",
        "Ensure competency in safety responsibilities and risk awareness"
      ]
    },
    {
      icon: MessageSquare,
      title: "Communication",
      color: "gold",
      items: [
        "Issue periodic safety bulletins and alerts",
        "Share lessons learned from investigations and industry events",
        "Maintain open, confidential channels for hazard reporting",
        "Reinforce risk-based decision making across all departments"
      ]
    },
    {
      icon: Heart,
      title: "Culture",
      color: "sky-blue",
      items: [
        "Promote a Just Culture that encourages transparent reporting",
        "Recognize positive safety behaviors and contributions",
        "Support continuous learning, teamwork, and proactive safety involvement"
      ]
    }
  ];

  const getColorClasses = (color: string) => {
    switch (color) {
      case 'primary':
        return { bg: 'bg-primary/10', text: 'text-primary', bullet: 'bg-primary' };
      case 'gold':
        return { bg: 'bg-gold/10', text: 'text-gold', bullet: 'bg-gold' };
      case 'sky-blue':
        return { bg: 'bg-sky-blue/10', text: 'text-sky-blue', bullet: 'bg-sky-blue' };
      default:
        return { bg: 'bg-primary/10', text: 'text-primary', bullet: 'bg-primary' };
    }
  };

  return (
    <section id="promotion" className="py-24 bg-muted/30">
      <div className="container mx-auto px-4">
        <div className="max-w-4xl mx-auto">
          <div className="flex items-center gap-3 mb-6">
            <div className="p-3 bg-primary/10 rounded-xl">
              <Megaphone className="h-8 w-8 text-primary" />
            </div>
            <h2 className="text-4xl font-bold text-foreground">Safety Promotion</h2>
          </div>

          <div className="bg-card rounded-2xl shadow-xl border border-border overflow-hidden">
            <div className="bg-gradient-to-r from-primary/80 to-aviation-green-light p-6">
              <h3 className="text-xl font-bold text-primary-foreground">Building Safety Culture</h3>
              <p className="text-primary-foreground/70 text-sm">Training, Communication & Engagement</p>
            </div>

            <div className="p-8">
              <p className="text-foreground/80 leading-relaxed mb-8 text-lg">
                STAR AIR Aviation fosters a strong safety culture through training, communication, and continuous engagement.
              </p>

              <div className="grid gap-6">
                {sections.map((section, index) => {
                  const colors = getColorClasses(section.color);
                  return (
                    <div key={index} className="bg-muted/30 rounded-xl p-6">
                      <div className="flex items-center gap-3 mb-4">
                        <div className={`p-2 ${colors.bg} rounded-lg`}>
                          <section.icon className={`h-5 w-5 ${colors.text}`} />
                        </div>
                        <h4 className="text-lg font-semibold text-foreground">{section.title}</h4>
                      </div>
                      <ul className="space-y-2 pl-4">
                        {section.items.map((item, itemIndex) => (
                          <li key={itemIndex} className="flex items-start gap-3">
                            <span className={`flex-shrink-0 w-2 h-2 ${colors.bullet} rounded-full mt-2`} />
                            <span className="text-foreground/80">{item}</span>
                          </li>
                        ))}
                      </ul>
                    </div>
                  );
                })}
              </div>
            </div>
          </div>
        </div>
      </div>
    </section>
  );
};
