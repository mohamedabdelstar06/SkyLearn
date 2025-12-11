import { useState, useEffect } from 'react';
import { Menu, X, Download } from 'lucide-react';
import { Button } from '@/components/ui/button';
import logo from '@/assets/star-air-logo.png';
import { generateFullPDF } from '@/lib/pdfGenerator';

const navItems = [
  { label: 'Home', href: '#home' },
  { label: 'Safety Policy', href: '#policy' },
  { label: 'SRM', href: '#srm' },
  { label: 'Safety Assurance', href: '#assurance' },
  { label: 'Safety Promotion', href: '#promotion' },
  { label: 'Risk Matrix', href: '#matrix' },
];

export const Navbar = () => {
  const [isOpen, setIsOpen] = useState(false);
  const [scrolled, setScrolled] = useState(false);

  useEffect(() => {
    const handleScroll = () => {
      setScrolled(window.scrollY > 50);
    };
    window.addEventListener('scroll', handleScroll);
    return () => window.removeEventListener('scroll', handleScroll);
  }, []);

  const handleDownload = () => {
    generateFullPDF();
  };

  return (
    <nav
      className={`fixed top-0 left-0 right-0 z-50 transition-all duration-300 ${
        scrolled ? 'bg-navy/95 backdrop-blur-lg shadow-xl' : 'bg-transparent'
      }`}
    >
      <div className="container mx-auto px-4">
        <div className="flex items-center justify-between h-20">
          <a href="#home" className="flex items-center gap-3">
            <img src={logo} alt="STAR AIR Logo" className="h-14 w-14 object-contain" />
            <span className="text-xl font-bold text-primary-foreground">STAR AIR</span>
          </a>

          {/* Desktop Navigation */}
          <div className="hidden lg:flex items-center gap-8">
            {navItems.map((item) => (
              <a
                key={item.href}
                href={item.href}
                className="text-primary-foreground/80 hover:text-primary transition-colors font-medium text-sm"
              >
                {item.label}
              </a>
            ))}
            <Button
              onClick={handleDownload}
              className="bg-primary hover:bg-primary/90 text-primary-foreground gap-2"
            >
              <Download className="h-4 w-4" />
              Download PDF
            </Button>
          </div>

          {/* Mobile Menu Button */}
          <button
            className="lg:hidden text-primary-foreground p-2"
            onClick={() => setIsOpen(!isOpen)}
          >
            {isOpen ? <X className="h-6 w-6" /> : <Menu className="h-6 w-6" />}
          </button>
        </div>

        {/* Mobile Navigation */}
        {isOpen && (
          <div className="lg:hidden bg-navy/95 backdrop-blur-lg rounded-b-2xl pb-6 animate-fade-in">
            <div className="flex flex-col gap-4 px-4">
              {navItems.map((item) => (
                <a
                  key={item.href}
                  href={item.href}
                  className="text-primary-foreground/80 hover:text-primary transition-colors font-medium py-2"
                  onClick={() => setIsOpen(false)}
                >
                  {item.label}
                </a>
              ))}
              <Button
                onClick={() => {
                  handleDownload();
                  setIsOpen(false);
                }}
                className="bg-primary hover:bg-primary/90 text-primary-foreground gap-2 mt-2"
              >
                <Download className="h-4 w-4" />
                Download PDF
              </Button>
            </div>
          </div>
        )}
      </div>
    </nav>
  );
};
