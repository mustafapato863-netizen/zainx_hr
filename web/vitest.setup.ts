// Vitest global test setup for ZainX Workforce web packages

// Provide a standard mock for HTMLCanvasElement.prototype.getContext in jsdom
// to prevent axe-core color contrast checks from emitting not-implemented warnings
if (typeof window !== 'undefined' && window.HTMLCanvasElement) {
  HTMLCanvasElement.prototype.getContext = function (contextId: string) {
    if (contextId === '2d') {
      return {
        fillRect: () => {},
        clearRect: () => {},
        getImageData: (x: number, y: number, w: number, h: number) => ({
          data: new Uint8ClampedArray(w * h * 4),
        }),
        putImageData: () => {},
        createImageData: () => [],
        setTransform: () => {},
        drawImage: () => {},
        save: () => {},
        fillText: () => {},
        restore: () => {},
        beginPath: () => {},
        moveTo: () => {},
        lineTo: () => {},
        closePath: () => {},
        stroke: () => {},
        translate: () => {},
        scale: () => {},
        rotate: () => {},
        arc: () => {},
        fill: () => {},
        measureText: () => ({ width: 0 }),
        transform: () => {},
        rect: () => {},
        clip: () => {},
      } as unknown as CanvasRenderingContext2D;
    }
    return null;
  } as unknown as typeof HTMLCanvasElement.prototype.getContext;
}

// Polyfill window.getComputedStyle pseudo element support in jsdom for axe-core
if (typeof window !== 'undefined') {
  const originalGetComputedStyle = window.getComputedStyle;
  window.getComputedStyle = function (elt: Element, pseudoElt?: string | null) {
    if (pseudoElt) {
      try {
        return originalGetComputedStyle.call(window, elt);
      } catch {
        return originalGetComputedStyle.call(window, elt);
      }
    }
    return originalGetComputedStyle.call(window, elt);
  };
}
