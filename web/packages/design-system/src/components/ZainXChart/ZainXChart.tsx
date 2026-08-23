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

/**
 * ZainXChart
 *
 * Encapsulates Apache ECharts behind a standardized semantic theme contract.
 *
 * ACCESSIBILITY & MULTIMODAL MANDATE:
 * Charts must NEVER be the sole representation of critical operational metrics.
 * ZainXChart automatically provides an accessible data table alternative
 * and screen-reader data summaries.
 */
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
            radius: ["45%", "75%"],
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
              },
            },
            data: data.map((d) => ({ name: d.label, value: d.value })),
          },
        ],
      }
    }

    return {
      ...baseConfig,
      xAxis: {
        type: "category",
        data: labels,
        inverse: isRtl,
        axisLine: { lineStyle: { color: gridLineColor } },
        axisLabel: { color: textColor },
      },
      yAxis: {
        type: "value",
        position: isRtl ? "right" : "left",
        splitLine: { lineStyle: { color: gridLineColor, type: "dashed" } },
        axisLabel: { color: textColor },
      },
      series: [
        {
          data: data.map((d) => d.value),
          type: type === "area" ? "line" : (type === "bar" || type === "stacked-bar" ? "bar" : "line"),
          smooth: type === "line" || type === "area",
          areaStyle: type === "area" ? { opacity: 0.25, color: primaryColor } : undefined,
          itemStyle: {
            color: primaryColor,
            borderRadius: type === "bar" ? [4, 4, 0, 0] : 0,
          },
        },
      ],
    }
  }, [type, data, customOptions, isRtl, isDark, primaryColor, secondaryColor, textColor, gridLineColor, surfaceColor, unit])

  return (
    <div className={cn("rounded-lg border border-border-default bg-surface p-4 shadow-xs", className)}>
      {/* Header with Title and Mode Toggle */}
      <div className="mb-3 flex items-center justify-between gap-2 border-b border-border-subtle pb-2.5">
        <div>
          {title && <h3 className="text-sm font-semibold text-text-primary">{title}</h3>}
          {description && <p className="text-xs text-text-tertiary">{description}</p>}
        </div>

        {allowTableView && (
          <Button
            variant="ghost"
            size="xs"
            onPress={() => setViewAsTable(!viewAsTable)}
            aria-label={viewAsTable ? "Switch to Graphical Chart" : "Switch to Accessible Table"}
          >
            <Icon name={viewAsTable ? "bar-chart-2" : "table"} size="xs" />
            <span>{viewAsTable ? (isRtl ? "رسم بياني" : "Chart View") : (isRtl ? "جدول البيانات" : "Table View")}</span>
          </Button>
        )}
      </div>

      {/* Screen-reader summary for non-visual assistive tech */}
      <div className="sr-only" aria-live="polite">
        {title ? `${title}: ` : ""}
        {data.map((d) => `${d.label}: ${d.value} ${unit}`).join(", ")}
      </div>

      {/* Content Rendering: Interactive Chart or Accessible Semantic Table */}
      {viewAsTable ? (
        <div className="overflow-x-auto max-h-[300px]">
          <Table>
            <TableHeader>
              <TableRow>
                <TableHead>{isRtl ? "البند / التاريخ" : "Item / Metric"}</TableHead>
                <TableHead>{isRtl ? "القيمة" : "Value"}</TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {data.map((item, idx) => (
                <TableRow key={idx}>
                  <TableCell>{item.label}</TableCell>
                  <TableCell className="font-medium">
                    {item.formattedValue || `${item.value} ${unit}`}
                  </TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
        </div>
      ) : (
        <div style={{ height }}>
          <ReactECharts
            option={option}
            style={{ height: "100%", width: "100%" }}
            opts={{ renderer: "svg" }}
            notMerge={true}
            lazyUpdate={true}
          />
        </div>
      )}
    </div>
  )
}
