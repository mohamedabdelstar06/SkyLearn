import jsPDF from 'jspdf';
import autoTable from 'jspdf-autotable';

interface RiskItem {
  id: number;
  hazard: string;
  consequence: string;
  severity: number;
  likelihood: number;
  riskLevel: string;
  mitigation: string;
  residualSeverity: number;
  residualLikelihood: number;
  residualRisk: string;
}

export const generateFullPDF = () => {
  const doc = new jsPDF();
  const pageWidth = doc.internal.pageSize.getWidth();
  let yPos = 20;

  // Title Page
  doc.setFontSize(28);
  doc.setTextColor(34, 45, 68);
  doc.text('STAR AIR AVIATION', pageWidth / 2, yPos, { align: 'center' });
  yPos += 15;
  
  doc.setFontSize(18);
  doc.setTextColor(74, 222, 128);
  doc.text('Safety Management System', pageWidth / 2, yPos, { align: 'center' });
  yPos += 10;
  
  doc.setFontSize(12);
  doc.setTextColor(100, 100, 100);
  doc.text('Compliant with ICAO Annex 19 Standards', pageWidth / 2, yPos, { align: 'center' });
  yPos += 20;

  // Safety Policy
  doc.setFontSize(16);
  doc.setTextColor(34, 45, 68);
  doc.text('1. SAFETY POLICY', 14, yPos);
  yPos += 10;

  doc.setFontSize(10);
  doc.setTextColor(60, 60, 60);
  const policyText = `Safety is the foundation of all our operations. STAR AIR Aviation is fully committed to achieving the highest levels of safety performance through the implementation and continuous enhancement of a comprehensive Safety Management System (SMS) in accordance with ICAO Annex 19 and all applicable national regulatory requirements.

To fulfil this commitment, STAR AIR Aviation will:
• Comply with all aviation safety laws, regulations, and standards
• Proactively identify hazards and manage associated risks
• Maintain an effective risk-based decision-making process in all activities
• Promote an open, Just Culture that encourages voluntary reporting without fear
• Allocate adequate resources, training, and technology to support safety
• Ensure timely communication of safety information across all departments
• Protect safety data and safeguard confidentiality of reporting sources
• Continuously measure, review, and improve safety performance

All employees, contractors, and partners share responsibility for safety and have full authority to stop or report any activity that may compromise safety.

Signed: Mohamed Abdelstar Abdelkader
Accountable Manager
Date: 30/12/2025`;

  const splitPolicy = doc.splitTextToSize(policyText, pageWidth - 28);
  doc.text(splitPolicy, 14, yPos);
  yPos += splitPolicy.length * 5 + 15;

  // Safety Risk Management
  doc.addPage();
  yPos = 20;
  doc.setFontSize(16);
  doc.setTextColor(34, 45, 68);
  doc.text('2. SAFETY RISK MANAGEMENT (SRM)', 14, yPos);
  yPos += 10;

  doc.setFontSize(10);
  doc.setTextColor(60, 60, 60);
  const srmText = `STAR AIR Aviation integrates Safety Risk Management (SRM) into all operational, maintenance, administrative, and organizational activities.

We will:
• Use proactive, reactive, and predictive methods to identify hazards
• Analyze risk based on standardized severity and likelihood classifications
• Determine and implement appropriate mitigation strategies aligned with acceptable risk levels
• Verify, monitor, and document the effectiveness of implemented controls
• Maintain a centralized hazard reporting and tracking system
• Conduct risk assessments for operational changes, new technologies, and procedures
• Ensure all risk assessments are recorded, traceable, and subject to periodic review

Hazard Identification Sources include:
• Operational and maintenance safety reports
• Audits, inspections, and investigations
• Weather, environment, and airport conditions
• Equipment and system performance data
• Feedback from employees, contractors, and stakeholders
• Voluntary and mandatory reporting systems`;

  const splitSRM = doc.splitTextToSize(srmText, pageWidth - 28);
  doc.text(splitSRM, 14, yPos);
  yPos += splitSRM.length * 5 + 15;

  // Safety Assurance
  doc.addPage();
  yPos = 20;
  doc.setFontSize(16);
  doc.setTextColor(34, 45, 68);
  doc.text('3. SAFETY ASSURANCE', 14, yPos);
  yPos += 10;

  doc.setFontSize(10);
  doc.setTextColor(60, 60, 60);
  const assuranceText = `Safety Assurance ensures that safety controls remain effective and the SMS functions as intended.

We achieve this by:
• Monitoring key safety performance indicators (SPIs) and trends
• Conducting scheduled and unscheduled internal audits
• Verifying regulatory compliance and operational standards
• Identifying root causes of safety deficiencies
• Validating the effectiveness of risk mitigations through follow-up actions
• Performing change-management safety reviews
• Analyzing data from reports, safety dashboards, and investigations
• Regularly reporting safety performance to senior leadership
• Ensuring continuous improvement of SMS processes`;

  const splitAssurance = doc.splitTextToSize(assuranceText, pageWidth - 28);
  doc.text(splitAssurance, 14, yPos);
  yPos += splitAssurance.length * 5 + 15;

  // Safety Promotion
  yPos += 10;
  doc.setFontSize(16);
  doc.setTextColor(34, 45, 68);
  doc.text('4. SAFETY PROMOTION', 14, yPos);
  yPos += 10;

  doc.setFontSize(10);
  doc.setTextColor(60, 60, 60);
  const promotionText = `STAR AIR Aviation fosters a strong safety culture through training, communication, and continuous engagement.

TRAINING:
• Provide SMS training tailored to each employee's role
• Conduct recurrent safety and refresher training programs
• Ensure competency in safety responsibilities and risk awareness

COMMUNICATION:
• Issue periodic safety bulletins and alerts
• Share lessons learned from investigations and industry events
• Maintain open, confidential channels for hazard reporting
• Reinforce risk-based decision making across all departments

CULTURE:
• Promote a Just Culture that encourages transparent reporting
• Recognize positive safety behaviors and contributions
• Support continuous learning, teamwork, and proactive safety involvement`;

  const splitPromotion = doc.splitTextToSize(promotionText, pageWidth - 28);
  doc.text(splitPromotion, 14, yPos);

  // Risk Matrix Page
  doc.addPage();
  yPos = 20;
  doc.setFontSize(16);
  doc.setTextColor(34, 45, 68);
  doc.text('5. SRM RISK MATRIX', 14, yPos);
  yPos += 10;

  const riskData = [
    [1, 'Bird strike on takeoff', 'Engine damage / rejected takeoff', 4, 4, 'High', 'Wildlife control, pilot briefing, radar monitoring', 3, 2, 'Medium'],
    [2, 'Runway incursion', 'Collision with aircraft/vehicle', 5, 3, 'High', 'Enhanced signage, surface radar, ATC coordination', 4, 1, 'Low'],
    [3, 'Fatigue among flight crew', 'Reduced situational awareness', 4, 3, 'High', 'Duty-time limits, fatigue reporting, roster review', 3, 2, 'Medium'],
    [4, 'Weather-related low visibility', 'Hard landing / unstable approach', 4, 4, 'High', 'Autoland systems, stricter minima, go-around policy', 3, 2, 'Medium'],
    [5, 'Miscommunication ATC-cockpit', 'Loss of separation', 4, 3, 'High', 'Standard phraseology, radio checks, CRM training', 3, 2, 'Medium'],
    [6, 'Maintenance error', 'System failure in flight', 5, 3, 'High', 'Dual inspections, digital checklists, staff training', 4, 2, 'Medium'],
    [7, 'Fuel miscalculation', 'In-flight fuel emergency', 5, 3, 'High', 'Cross-checking, automated calculations, dispatch checks', 4, 2, 'Medium'],
    [8, 'Cabin crew insufficient training', 'Poor emergency response', 4, 3, 'High', 'Recurrent training, standardized procedures', 3, 2, 'Medium'],
    [9, 'Ground handling congestion', 'Vehicle collision / delay', 3, 3, 'Medium', 'Marked zones, staff coordination, slow-speed rules', 3, 1, 'Low'],
  ];

  autoTable(doc, {
    startY: yPos,
    head: [['ID', 'Hazard', 'Consequence', 'Sev.', 'Lik.', 'Risk', 'Mitigation', 'R.Sev', 'R.Lik', 'R.Risk']],
    body: riskData,
    theme: 'striped',
    headStyles: { fillColor: [34, 45, 68], fontSize: 7 },
    bodyStyles: { fontSize: 6 },
    columnStyles: {
      0: { cellWidth: 8 },
      1: { cellWidth: 25 },
      2: { cellWidth: 25 },
      3: { cellWidth: 10 },
      4: { cellWidth: 10 },
      5: { cellWidth: 15 },
      6: { cellWidth: 40 },
      7: { cellWidth: 12 },
      8: { cellWidth: 12 },
      9: { cellWidth: 15 },
    },
  });

  doc.save('STAR_AIR_SMS_Complete.pdf');
};

export const generateRiskMatrixPDF = (data: RiskItem[]) => {
  const doc = new jsPDF('landscape');
  const pageWidth = doc.internal.pageSize.getWidth();
  let yPos = 20;

  doc.setFontSize(20);
  doc.setTextColor(34, 45, 68);
  doc.text('STAR AIR - SRM Risk Matrix', pageWidth / 2, yPos, { align: 'center' });
  yPos += 15;

  const tableData = data.map(item => [
    item.id,
    item.hazard,
    item.consequence,
    item.severity,
    item.likelihood,
    item.riskLevel,
    item.mitigation,
    item.residualSeverity,
    item.residualLikelihood,
    item.residualRisk
  ]);

  autoTable(doc, {
    startY: yPos,
    head: [['ID', 'Hazard', 'Consequence', 'Severity', 'Likelihood', 'Risk Level', 'Mitigation', 'Res. Severity', 'Res. Likelihood', 'Res. Risk']],
    body: tableData,
    theme: 'striped',
    headStyles: { fillColor: [34, 45, 68], fontSize: 9 },
    bodyStyles: { fontSize: 8 },
    didParseCell: function(data) {
      if (data.section === 'body') {
        if (data.column.index === 5 || data.column.index === 9) {
          const value = data.cell.raw as string;
          if (value === 'High') {
            data.cell.styles.textColor = [220, 38, 38];
          } else if (value === 'Medium') {
            data.cell.styles.textColor = [245, 158, 11];
          } else if (value === 'Low') {
            data.cell.styles.textColor = [34, 197, 94];
          }
        }
      }
    }
  });

  doc.save('STAR_AIR_Risk_Matrix.pdf');
};
