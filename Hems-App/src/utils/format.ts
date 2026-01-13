

/**
  @param v
 * @returns 
 */
export const fmtKWh = (v: number): string => {
    if (!Number.isFinite(v)) return "0";
    const s = v.toFixed(2);
    return s.replace(/\.00$/, "").replace(/(\.\d)0$/, "$1");
};

/**
 * @param p 
 * @returns 
 */
export const fmtPct01 = (p?: number): string => {
    if (p == null || !Number.isFinite(p)) return "0%";
    const s = (p * 100).toFixed(2);
    return (s.replace(/\.00$/, "").replace(/(\.\d)0$/, "$1")) + "%";
};

/**
 * @param p 
 * @returns 
 */
export const fmtPct = (p: number): string => {
    const percent = p.toFixed(2);
    return percent.replace(/\.00$/, '') + '%';
};
