import logo from '@/assets/star-air-logo.png';

export const Footer = () => {
  return (
    <footer className="bg-navy py-12">
      <div className="container mx-auto px-4">
        <div className="flex flex-col md:flex-row items-center justify-between gap-6">
          <div className="flex items-center gap-3">
            <img src={logo} alt="STAR AIR Logo" className="h-12 w-12 object-contain" />
            <div>
              <span className="text-xl font-bold text-primary-foreground">STAR AIR</span>
              <p className="text-primary-foreground/60 text-sm">Safety Management System</p>
            </div>
          </div>
          
          <div className="text-center md:text-right">
            <p className="text-primary-foreground/60 text-sm">
              Compliant with ICAO Annex 19 Standards
            </p>
            <p className="text-primary-foreground/40 text-xs mt-2">
              © 2025 STAR AIR Aviation. All rights reserved.
            </p>
          </div>
        </div>
      </div>
    </footer>
  );
};
