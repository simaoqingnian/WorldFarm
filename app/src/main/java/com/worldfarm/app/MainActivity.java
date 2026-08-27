package com.worldfarm.app;

import android.app.Activity;
import android.graphics.Color;
import android.graphics.Typeface;
import android.graphics.drawable.GradientDrawable;
import android.os.Bundle;
import android.view.Gravity;
import android.view.View;
import android.view.ViewGroup;
import android.widget.GridLayout;
import android.widget.LinearLayout;
import android.widget.ScrollView;
import android.widget.TextView;

import java.text.SimpleDateFormat;
import java.util.Date;
import java.util.Locale;

public class MainActivity extends Activity {
    private static final int BACKGROUND = Color.rgb(245, 247, 242);
    private static final int SURFACE = Color.WHITE;
    private static final int PRIMARY = Color.rgb(37, 77, 59);
    private static final int TEXT = Color.rgb(24, 37, 30);
    private static final int MUTED = Color.rgb(94, 109, 100);
    private static final int GOLD = Color.rgb(222, 169, 62);
    private static final int BLUE = Color.rgb(75, 124, 184);
    private static final int RED = Color.rgb(203, 88, 72);

    @Override
    protected void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);

        ScrollView scrollView = new ScrollView(this);
        scrollView.setFillViewport(true);
        scrollView.setBackgroundColor(BACKGROUND);

        LinearLayout root = new LinearLayout(this);
        root.setOrientation(LinearLayout.VERTICAL);
        root.setPadding(dp(20), dp(18), dp(20), dp(28));
        scrollView.addView(root, new ScrollView.LayoutParams(
                ViewGroup.LayoutParams.MATCH_PARENT,
                ViewGroup.LayoutParams.WRAP_CONTENT
        ));

        addHeader(root);
        addStatusRow(root);
        addFarmGrid(root);
        addWorldMap(root);
        addOverlapPolicy(root);

        setContentView(scrollView);
    }

    private void addHeader(LinearLayout root) {
        TextView title = text("WorldFarm", 30, TEXT, Typeface.BOLD);
        root.addView(title, matchWrap());

        TextView subtitle = text("中国农场 · 世界版图", 15, MUTED, Typeface.NORMAL);
        LinearLayout.LayoutParams subtitleParams = matchWrap();
        subtitleParams.topMargin = dp(4);
        root.addView(subtitle, subtitleParams);
    }

    private void addStatusRow(LinearLayout root) {
        LinearLayout row = new LinearLayout(this);
        row.setOrientation(LinearLayout.HORIZONTAL);
        row.setGravity(Gravity.CENTER_VERTICAL);
        row.setPadding(0, dp(18), 0, dp(10));

        row.addView(chip("Lv.1", PRIMARY, Color.WHITE), weightWrap(1));
        row.addView(chip("金币 260", GOLD, TEXT), weightWrap(1));
        row.addView(chip(localClock(), BLUE, Color.WHITE), weightWrap(1));

        root.addView(row, matchWrap());
    }

    private void addFarmGrid(LinearLayout root) {
        root.addView(sectionTitle("中国地块"), spacedTitle());

        GridLayout grid = new GridLayout(this);
        grid.setColumnCount(2);
        grid.setUseDefaultMargins(false);

        addPlot(grid, "华北麦田", "小麦", "02:15 成熟", PRIMARY);
        addPlot(grid, "江南水田", "水稻", "可收获", GOLD);
        addPlot(grid, "岭南菜畦", "白菜", "00:48 成熟", BLUE);
        addPlot(grid, "西北试验田", "锁定", "声望 2", MUTED);

        root.addView(grid, matchWrap());
    }

    private void addWorldMap(LinearLayout root) {
        root.addView(sectionTitle("世界地图"), spacedTitle());

        LinearLayout card = card();
        TextView headline = text("下一块版图可自由选择", 18, TEXT, Typeface.BOLD);
        card.addView(headline, matchWrap());

        TextView body = text("完成中国新手目标后开启全球通行证。日本、泰国、墨西哥、法国等国家可以并行解锁，不要求先清空中国作物图鉴。", 14, MUTED, Typeface.NORMAL);
        body.setLineSpacing(dp(2), 1.0f);
        LinearLayout.LayoutParams bodyParams = matchWrap();
        bodyParams.topMargin = dp(8);
        card.addView(body, bodyParams);

        LinearLayout row = new LinearLayout(this);
        row.setOrientation(LinearLayout.HORIZONTAL);
        row.setPadding(0, dp(14), 0, 0);
        row.addView(chip("日本", RED, Color.WHITE), weightWrap(1));
        row.addView(chip("泰国", PRIMARY, Color.WHITE), weightWrap(1));
        row.addView(chip("墨西哥", GOLD, TEXT), weightWrap(1));
        card.addView(row, matchWrap());

        root.addView(card, matchWrap());
    }

    private void addOverlapPolicy(LinearLayout root) {
        root.addView(sectionTitle("重叠作物"), spacedTitle());

        LinearLayout card = card();
        card.addView(text("同一物种，不同国家品种", 18, TEXT, Typeface.BOLD), matchWrap());

        TextView body = text("例如水稻作为物种进入图鉴；中国地块种中国水稻，日本地块种日本粳米，泰国地块种茉莉香米。产物可共享基础用途，也保留国家标签用于订单和料理。", 14, MUTED, Typeface.NORMAL);
        body.setLineSpacing(dp(2), 1.0f);
        LinearLayout.LayoutParams bodyParams = matchWrap();
        bodyParams.topMargin = dp(8);
        card.addView(body, bodyParams);

        root.addView(card, matchWrap());
    }

    private void addPlot(GridLayout grid, String landName, String cropName, String status, int accent) {
        LinearLayout plot = card();
        plot.setMinimumHeight(dp(132));

        TextView land = text(landName, 15, TEXT, Typeface.BOLD);
        plot.addView(land, matchWrap());

        TextView crop = text(cropName, 24, accent, Typeface.BOLD);
        LinearLayout.LayoutParams cropParams = matchWrap();
        cropParams.topMargin = dp(12);
        plot.addView(crop, cropParams);

        TextView state = text(status, 13, MUTED, Typeface.NORMAL);
        LinearLayout.LayoutParams stateParams = matchWrap();
        stateParams.topMargin = dp(10);
        plot.addView(state, stateParams);

        GridLayout.LayoutParams params = new GridLayout.LayoutParams();
        params.width = 0;
        params.height = ViewGroup.LayoutParams.WRAP_CONTENT;
        params.columnSpec = GridLayout.spec(GridLayout.UNDEFINED, 1f);
        params.setMargins(dp(0), dp(0), dp(10), dp(10));
        grid.addView(plot, params);
    }

    private LinearLayout card() {
        LinearLayout view = new LinearLayout(this);
        view.setOrientation(LinearLayout.VERTICAL);
        view.setPadding(dp(16), dp(14), dp(16), dp(14));

        GradientDrawable background = new GradientDrawable();
        background.setColor(SURFACE);
        background.setCornerRadius(dp(8));
        background.setStroke(dp(1), Color.rgb(225, 231, 224));
        view.setBackground(background);
        view.setClipToOutline(true);

        return view;
    }

    private TextView chip(String label, int backgroundColor, int textColor) {
        TextView view = text(label, 13, textColor, Typeface.BOLD);
        view.setGravity(Gravity.CENTER);
        view.setSingleLine(true);
        view.setPadding(dp(8), dp(8), dp(8), dp(8));

        GradientDrawable background = new GradientDrawable();
        background.setColor(backgroundColor);
        background.setCornerRadius(dp(8));
        view.setBackground(background);

        return view;
    }

    private TextView sectionTitle(String value) {
        TextView view = text(value, 18, TEXT, Typeface.BOLD);
        view.setGravity(Gravity.CENTER_VERTICAL);
        return view;
    }

    private TextView text(String value, int sp, int color, int style) {
        TextView view = new TextView(this);
        view.setText(value);
        view.setTextSize(sp);
        view.setTextColor(color);
        view.setTypeface(Typeface.DEFAULT, style);
        view.setIncludeFontPadding(true);
        return view;
    }

    private String localClock() {
        return new SimpleDateFormat("HH:mm", Locale.getDefault()).format(new Date());
    }

    private LinearLayout.LayoutParams matchWrap() {
        return new LinearLayout.LayoutParams(
                ViewGroup.LayoutParams.MATCH_PARENT,
                ViewGroup.LayoutParams.WRAP_CONTENT
        );
    }

    private LinearLayout.LayoutParams spacedTitle() {
        LinearLayout.LayoutParams params = matchWrap();
        params.topMargin = dp(16);
        params.bottomMargin = dp(10);
        return params;
    }

    private LinearLayout.LayoutParams weightWrap(float weight) {
        LinearLayout.LayoutParams params = new LinearLayout.LayoutParams(0, ViewGroup.LayoutParams.WRAP_CONTENT, weight);
        params.setMargins(dp(0), 0, dp(8), 0);
        return params;
    }

    private int dp(int value) {
        return Math.round(value * getResources().getDisplayMetrics().density);
    }
}
