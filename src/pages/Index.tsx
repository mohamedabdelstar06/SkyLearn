import { Navbar } from '@/components/Navbar';
import { HeroSection } from '@/components/HeroSection';
import { SafetyPolicy } from '@/components/SafetyPolicy';
import { SafetyRiskManagement } from '@/components/SafetyRiskManagement';
import { SafetyAssurance } from '@/components/SafetyAssurance';
import { SafetyPromotion } from '@/components/SafetyPromotion';
import { RiskMatrix } from '@/components/RiskMatrix';
import { Footer } from '@/components/Footer';

const Index = () => {
  return (
    <main className="min-h-screen">
      <Navbar />
      <HeroSection />
      <SafetyPolicy />
      <SafetyRiskManagement />
      <SafetyAssurance />
      <SafetyPromotion />
      <RiskMatrix />
      <Footer />
    </main>
  );
};

export default Index;
