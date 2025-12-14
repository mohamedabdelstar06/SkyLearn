import { useState, useEffect } from 'react';
import { X, AlertTriangle } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Textarea } from '@/components/ui/textarea';

interface HazardFormData {
  hazard: string;
  consequence: string;
  severity: number;
  likelihood: number;
  mitigation: string;
  residualSeverity: number;
  residualLikelihood: number;
}

interface HazardFormDialogProps {
  isOpen: boolean;
  onClose: () => void;
  onSubmit: (data: HazardFormData) => void;
  initialData?: HazardFormData;
  mode: 'add' | 'edit';
}

const calculateRiskLevel = (severity: number, likelihood: number): 'High' | 'Medium' | 'Low' => {
  const score = severity * likelihood;
  if (score >= 12) return 'High';
  if (score >= 5) return 'Medium';
  return 'Low';
};

const getRiskColor = (level: 'High' | 'Medium' | 'Low') => {
  switch (level) {
    case 'High': return 'text-risk-high';
    case 'Medium': return 'text-risk-medium';
    case 'Low': return 'text-risk-low';
  }
};

export const HazardFormDialog = ({ isOpen, onClose, onSubmit, initialData, mode }: HazardFormDialogProps) => {
  const [formData, setFormData] = useState<HazardFormData>({
    hazard: '',
    consequence: '',
    severity: 1,
    likelihood: 1,
    mitigation: '',
    residualSeverity: 1,
    residualLikelihood: 1,
  });

  useEffect(() => {
    if (initialData) {
      setFormData(initialData);
    } else {
      setFormData({
        hazard: '',
        consequence: '',
        severity: 1,
        likelihood: 1,
        mitigation: '',
        residualSeverity: 1,
        residualLikelihood: 1,
      });
    }
  }, [initialData, isOpen]);

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    onSubmit(formData);
    onClose();
  };

  const riskLevel = calculateRiskLevel(formData.severity, formData.likelihood);
  const residualRisk = calculateRiskLevel(formData.residualSeverity, formData.residualLikelihood);
  const riskScore = formData.severity * formData.likelihood;
  const residualScore = formData.residualSeverity * formData.residualLikelihood;

  if (!isOpen) return null;

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center p-4 animate-fade-in">
      {/* Backdrop */}
      <div 
        className="absolute inset-0 bg-background/80 backdrop-blur-sm"
        onClick={onClose}
      />
      
      {/* Dialog */}
      <div className="relative bg-card border border-border rounded-2xl shadow-2xl w-full max-w-2xl max-h-[90vh] overflow-y-auto animate-scale-in">
        {/* Header */}
        <div className="sticky top-0 bg-navy text-primary-foreground px-6 py-4 flex items-center justify-between rounded-t-2xl">
          <div className="flex items-center gap-3">
            <AlertTriangle className="h-5 w-5" />
            <h3 className="text-lg font-semibold">
              {mode === 'add' ? 'Add New Hazard' : 'Edit Hazard'}
            </h3>
          </div>
          <button
            onClick={onClose}
            className="p-1 hover:bg-white/10 rounded-lg transition-colors"
          >
            <X className="h-5 w-5" />
          </button>
        </div>

        {/* Form */}
        <form onSubmit={handleSubmit} className="p-6 space-y-6">
          {/* Hazard & Consequence */}
          <div className="grid gap-4 md:grid-cols-2">
            <div className="space-y-2">
              <Label htmlFor="hazard" className="text-foreground">Hazard *</Label>
              <Input
                id="hazard"
                value={formData.hazard}
                onChange={(e) => setFormData({ ...formData, hazard: e.target.value })}
                placeholder="e.g., Bird strike on takeoff"
                required
                className="bg-background border-border"
              />
            </div>
            <div className="space-y-2">
              <Label htmlFor="consequence" className="text-foreground">Consequence *</Label>
              <Input
                id="consequence"
                value={formData.consequence}
                onChange={(e) => setFormData({ ...formData, consequence: e.target.value })}
                placeholder="e.g., Engine damage"
                required
                className="bg-background border-border"
              />
            </div>
          </div>

          {/* Initial Risk Assessment */}
          <div className="p-4 bg-muted/30 rounded-xl border border-border space-y-4">
            <h4 className="font-semibold text-foreground flex items-center gap-2">
              Initial Risk Assessment
              <span className={`ml-auto px-3 py-1 rounded-full text-sm font-bold ${getRiskColor(riskLevel)} bg-current/10`}>
                {riskLevel} ({riskScore})
              </span>
            </h4>
            <div className="grid gap-4 md:grid-cols-2">
              <div className="space-y-2">
                <Label htmlFor="severity" className="text-foreground">Severity (1-5) *</Label>
                <Input
                  id="severity"
                  type="number"
                  min={1}
                  max={5}
                  value={formData.severity}
                  onChange={(e) => setFormData({ ...formData, severity: Math.min(5, Math.max(1, parseInt(e.target.value) || 1)) })}
                  required
                  className="bg-background border-border"
                />
              </div>
              <div className="space-y-2">
                <Label htmlFor="likelihood" className="text-foreground">Likelihood (1-5) *</Label>
                <Input
                  id="likelihood"
                  type="number"
                  min={1}
                  max={5}
                  value={formData.likelihood}
                  onChange={(e) => setFormData({ ...formData, likelihood: Math.min(5, Math.max(1, parseInt(e.target.value) || 1)) })}
                  required
                  className="bg-background border-border"
                />
              </div>
            </div>
          </div>

          {/* Mitigation */}
          <div className="space-y-2">
            <Label htmlFor="mitigation" className="text-foreground">Mitigation Measures *</Label>
            <Textarea
              id="mitigation"
              value={formData.mitigation}
              onChange={(e) => setFormData({ ...formData, mitigation: e.target.value })}
              placeholder="e.g., Wildlife control, pilot briefing, radar monitoring"
              required
              rows={3}
              className="bg-background border-border resize-none"
            />
          </div>

          {/* Residual Risk Assessment */}
          <div className="p-4 bg-aviation-green/5 rounded-xl border border-aviation-green/20 space-y-4">
            <h4 className="font-semibold text-foreground flex items-center gap-2">
              Residual Risk (After Mitigation)
              <span className={`ml-auto px-3 py-1 rounded-full text-sm font-bold ${getRiskColor(residualRisk)} bg-current/10`}>
                {residualRisk} ({residualScore})
              </span>
            </h4>
            <div className="grid gap-4 md:grid-cols-2">
              <div className="space-y-2">
                <Label htmlFor="residualSeverity" className="text-foreground">Residual Severity (1-5) *</Label>
                <Input
                  id="residualSeverity"
                  type="number"
                  min={1}
                  max={5}
                  value={formData.residualSeverity}
                  onChange={(e) => setFormData({ ...formData, residualSeverity: Math.min(5, Math.max(1, parseInt(e.target.value) || 1)) })}
                  required
                  className="bg-background border-border"
                />
              </div>
              <div className="space-y-2">
                <Label htmlFor="residualLikelihood" className="text-foreground">Residual Likelihood (1-5) *</Label>
                <Input
                  id="residualLikelihood"
                  type="number"
                  min={1}
                  max={5}
                  value={formData.residualLikelihood}
                  onChange={(e) => setFormData({ ...formData, residualLikelihood: Math.min(5, Math.max(1, parseInt(e.target.value) || 1)) })}
                  required
                  className="bg-background border-border"
                />
              </div>
            </div>
          </div>

          {/* Actions */}
          <div className="flex gap-3 justify-end pt-4 border-t border-border">
            <Button type="button" variant="outline" onClick={onClose}>
              Cancel
            </Button>
            <Button type="submit" className="bg-primary hover:bg-primary/90">
              {mode === 'add' ? 'Add Hazard' : 'Save Changes'}
            </Button>
          </div>
        </form>
      </div>
    </div>
  );
};
