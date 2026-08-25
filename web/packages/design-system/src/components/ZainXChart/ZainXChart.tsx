import * as React from "react"
import ReactECharts from "echarts-for-react"
import type { EChartsOption } from "echarts"
import { cn } from "../../lib/utils"
import { Button } from "../Button/Button"
import { Icon } from "../Icon/Icon"
import { Table, TableHeader, TableRow, TableHead, TableBody, TableCell } from "../Table/Table"

export interface ZainXChartDataPoint {
  label: string
  value: number
  category?: string
  formattedValue?: string
}

export interface ZainXChartProps {
  className?: string
  title?: string
  description?: string
  type: "line" | "bar" | "stacked-bar" | "area" | "donut" | "time-series"
  data: ZainXChartDataPoint[]
  categories?: string[]
  height?: string | number
  isRtl?: boolean
  isDark?: boolean
  unit?: string
  allowTableView?: boolean
  customOptions?: EChartsOption
}

export function ZainXChart({
  className,
  title,
  description,
  type,
  data = [],
  categories = [],
  height = "320px",
  isRtl = false,
  isDark = false,
  unit = "",
  allowTableView = true,
  customOptions,
}: ZainXChartProps) {
  const [viewAsTable, setViewAsTable] = React.useState(false)

  // Derive semantic theme colors
  const primaryColor = isDark ? "#38bdf8" : "#0284c7"
  const secondaryColor = isDark ? "#818cf8" : "#4f46e5"
  const textColor = isDark ? "#f1f5f9" : "#0f172a"
  const gridLineColor = isDark ? "#334155" : "#e2e8f0"
  const surfaceColor = isDark ? "#1e293b" : "#ffffff"

  const option = React.useMemo<EChartsOption>(() => {
    if (customOptions) return customOptions

    const labels = Array.from(new Set(data.map((d) => d.label)))

    const baseConfig: EChartsOption = {
      backgroundColor: "transparent",
      textStyle: {
        fontFamily: isRtl ? "inherit" : "-apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif",
        color: textColor,
      },
      tooltip: {
        trigger: type === "donut" ? "item" : "axis",
        backgroundColor: surfaceColor,
        borderColor: gridLineColor,
        textStyle: { color: textColor },
        formatter: (params: any) => {
          if (Array.isArray(params)) {
            const item = params[0]
            return `<div class="${isRtl ? 'text-right' : 'text-left'} text-xs font-medium">
              <div>${item.name}</div>
              <div class="font-bold text-primary">${item.value} ${unit}</div>
            </div>`
          }
          return `<div class="${isRtl ? 'text-right' : 'text-left'} text-xs font-medium">
            <div>${params.name}</div>
            <div class="font-bold text-primary">${params.value} ${unit} (${params.percent}%)</div>
          </div>`
        },
      },
      grid: {
        left: isRtl ? "5%" : "8%",
        right: isRtl ? "8%" : "5%",
        top: "12%",
        bottom: "12%",
        containLabel: true,
      },
    }

    if (type === "donut") {
      return {
        ...baseConfig,
        series: [
          {
            type: "pie",
            radius: ["50%", "75%"],
            avoidLabelOverlap: false,
            itemStyle: {
              borderRadius: 6,
              borderColor: surfaceColor,
              borderWidth: 2,
            },
            label: {
              show: false,
              position: "center",
            },
            emphasis: {
              label: {
                show: true,
                fontSize: 14,
                fontWeight: "bold",
                color: textColor,
              },
            },
            data: data.map((d) => ({ name: d.label, value: d.value })),
          },
        ],
      }
    }

    if (type === "stacked-bar" && categories.length > 0) {
      return {
        ...baseConfig,
        legend: {
          top: "top",
          textStyle: { color: textColor },
        },
        xAxis: {
          type: "category",
          data: labels,
          axisLine: { lineStyle: { color: gridLineColor } },
          axisLabel: { color: textColor },
        },
        yAxis: {
          type: "value",
          splitLine: { lineStyle: { color: gridLineColor, type: "dashed" } },
          axisLabel: { color: textColor },
        },
        series: categories.map((cat, idx) => ({
          name: cat,
          type: "bar",
          stack: "total",
          data: labels.map((l) => data.find((d) => d.label === l && d.category === cat)?.value || 0),
          itemStyle: {
            color: idx === 0 ? primaryColor : idx === 1 ? secondaryColor : "#10b981",
          },
        })),
      }
    }

    return {
      ...baseConfig,
      xAxis: {
        type: "category",
        data: labels,
        axisLine: { lineStyle: { color: gridLineColor } },
        axisLabel: { color: textColor },
      },
      yAxis: {
        type: "value",
        splitLine: { lineStyle: { color: gridLineColor, type: "dashed" } },
        axisLabel: { color: textColor },
      },
      series: [
        {
          data: data.map((d) => d.value),
          type: type === "area" || type === "time-series" ? "line" : (type as any),
          smooth: type === "area" || type === "time-series",
          areaStyle: type === "area" || type === "time-series" ? { opacity: 0.2, color: primaryColor } : undefined,
          itemStyle: { color: primaryColor },
          lineStyle: { width: 3, color: primaryColor },
        },
      ],
    }
  }, [customOptions, data, categories, type, isRtl, textColor, surfaceColor, gridLineColor, primaryColor, secondaryColor, unit])

  return (
    <div className={cn("rounded-lg border border-border-default bg-surface p-4 shadow-xs", className)}>
      {/* Header & Accessibility Mode Switcher */}
      <div className="mb-4 flex items-center justify-between gap-4 border-b border-border-subtle pb-3">
        <div>
          {title && <h3 className="text-base font-semibold text-text-primary">{title}</h3>}
          {description && <p className="text-xs text-text-secondary">{description}</p>}
        </div>

        {allowTableView && (
          <div className="flex items-center gap-2">
            <Button
              variant={viewAsTable ? "secondary" : "ghost"}
              size="xs"
              aria-label={viewAsTable ? "Switch to graphical view" : "Switch to accessible table"}
              onPress={() => setViewAsTable(!viewAsTable)}
            >
              <Icon name={viewAsTable ? "bar-chart-2" : "table"} size="xs" />
              <span className="ms-1.5 hidden sm:inline">
                {viewAsTable ? (isRtl ? "عرض الرسم البياني" : "View Chart") : (isRtl ? "عرض كجدول" : "View Table")}
              </span>
            </Button>
          </div>
        )}
      </div>

      {/* Graphical / Accessible Table Rendering */}
      {viewAsTable ? (
        <div className="overflow-x-auto" style={{ minHeight: height }}>
          <Table aria-label={title || "Chart Data Table"}>
            <TableHeader>
              <TableRow>
                <TableHead>{isRtl ? "الفئة / التسمية" : "Category / Label"}</TableHead>
                {categories.length > 0 && <TableHead>{isRtl ? "النوع" : "Group"}</TableHead>}
                <TableHead>{isRtl ? "القيمة" : "Value"}</TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {data.map((dp, idx) => (
                <TableRow key={`${dp.label}-${dp.category || idx}`}>
                  <TableCell className="font-medium">{dp.label}</TableCell>
                  {categories.length > 0 && <TableCell>{dp.category || "-"}</TableCell>}
                  <TableCell className="font-mono">{dp.formattedValue || `${dp.value} ${unit}`}</TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
        </div>
      ) : (
        <div style={{ height, width: "100%" }} role="img" aria-label={title || "Operational Chart"}>
          <ReactECharts option={option} style={{ height: "100%", width: "100%" }} opts={{ renderer: "svg" }} />
        </div>
      )}
    </div>
  )
}
