import { ChevronDown, Shield, Plane } from 'lucide-react';
import heroImage from '@/assets/hero-aviation.jpg';

export const HeroSection = () => {
  return (
    <section id="home" className="relative min-h-screen flex items-center justify-center overflow-hidden">
      {/* Background Image */}
      <div className="absolute inset-0">
        <img
          src={heroImage}
          alt="Aviation Safety"
          className="w-full h-full object-cover"
        />
        <div className="absolute inset-0 bg-gradient-to-b from-navy/80 via-navy/60 to-navy/90" />
      </div>

      {/* Content */}
      <div className="relative z-10 container mx-auto px-4 pt-32 pb-20">
        <div className="max-w-4xl mx-auto text-center">
          <div className="flex items-center justify-center gap-3 mb-6 animate-fade-up">
            <Shield className="h-8 w-8 text-primary" />
            <span className="text-primary font-semibold tracking-wider uppercase text-sm">
              Safety Management System
            </span>
            <Plane className="h-8 w-8 text-primary" />
          </div>
          
          <h1 className="text-5xl md:text-7xl font-bold text-primary-foreground mb-6 animate-fade-up" style={{ animationDelay: '0.1s' }}>
            STAR AIR
          </h1>
          
          <p className="text-xl md:text-2xl text-primary-foreground/80 mb-4 animate-fade-up" style={{ animationDelay: '0.2s' }}>
            Safety Component Program
          </p>
          
          <p className="text-lg text-primary-foreground/60 mb-12 max-w-2xl mx-auto animate-fade-up" style={{ animationDelay: '0.3s' }}>
            Committed to achieving the highest levels of aviation safety through comprehensive risk management, continuous improvement, and a strong safety culture.
          </p>

          <div className="flex flex-wrap items-center justify-center gap-4 animate-fade-up" style={{ animationDelay: '0.4s' }}>
            <a
              href="#policy"
              className="bg-primary hover:bg-primary/90 text-primary-foreground px-8 py-4 rounded-lg font-semibold transition-all duration-300 hover:scale-105 shadow-lg shadow-primary/30"
            >
              Explore Our SMS
            </a>
            <a
              href="#matrix"
              className="glass-effect text-primary-foreground px-8 py-4 rounded-lg font-semibold transition-all duration-300 hover:bg-white/20"
            >
              View Risk Matrix
            </a>
          </div>
        </div>
      </div>

      {/* Scroll Indicator */}
      <div className="absolute bottom-8 left-1/2 -translate-x-1/2 animate-float">
        <a href="#policy" className="flex flex-col items-center gap-2 text-primary-foreground/60 hover:text-primary transition-colors">
          <span className="text-sm font-medium">Scroll Down</span>
          <ChevronDown className="h-6 w-6" />
        </a>
      </div>
    </section>
  );
};
