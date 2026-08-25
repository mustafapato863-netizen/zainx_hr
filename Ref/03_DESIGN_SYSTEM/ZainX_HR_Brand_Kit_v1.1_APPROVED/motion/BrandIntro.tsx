import './brand-intro.css';

export function BrandIntro() {
  return (
    <div className="zainx-intro" role="status" aria-label="Loading Zain X HR">
      <img
        className="zainx-intro__mark"
        src="/brand/logos/zainx-hr-mark.webp"
        alt=""
        aria-hidden="true"
      />
      <div className="zainx-intro__copy">
        <span className="zainx-intro__name">Zain X HR</span>
        <span className="zainx-intro__tagline">Human Resources Platform</span>
      </div>
    </div>
  );
}
