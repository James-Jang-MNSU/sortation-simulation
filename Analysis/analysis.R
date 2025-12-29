# 1. Load Libraries

#install.packages("here")
#install.packages("tidyverse")
#install.packages("car")
library(tidyverse)
library(here)
library(car)

#-------------------------------------------------------------------------------

# 2. Load Data

csv_path <- here("SortationEngine", "bin", "Debug", "net8.0", "simulation_data.csv")
df <- read.csv(csv_path)
df$Success <- as.logical(df$Success)

#-------------------------------------------------------------------------------

# 3. Data Quality Check

# Structure and Column types
glimpse(df)

# Count total missing values
sum(is.na(df))

# Find and print any invalid rows (negative truck count or duration)
invalid_runs <- df %>%
  filter(TotalTrucks <= 0 | Duration <= 0) 
print(invalid_runs)

#-------------------------------------------------------------------------------

# 4. Exploratory Data Analysis

# Entire summary
df %>% select(-RunId) %>% summary() 

# Survival & Failure Rate (%)
survival_data <- df %>%
  summarise(
    Total_Runs = n(),
    Total_Successes = sum(Success),
    Survival_Rate_Percent = mean(Success) * 100,
    Failure_Rate_Percent = (1-mean(Success)) * 100
  )
print(survival_data)

# Standard Deviations (Imbalance)
metric_spread <- df %>%
  select(where(is.numeric)) %>%
  select(-RunId, -Duration, -MaxStationLoad) %>%
  summarise(across(everything(), sd)) %>%
  pivot_longer(everything(), names_to = "Metric", values_to = "Std_Dev") %>%
  mutate(Std_Dev = signif(Std_Dev, 4)) %>%
  arrange(desc(Std_Dev))
print(metric_spread)

# Box Plots: Failure vs Success
long_data <- df %>%
  select(Success, Duration, TotalTrucks, TotalParcels, MinInterarrival, MaxBeltLoad, StationLoadStdDev) %>%
  pivot_longer(cols = -Success, names_to = "Metric", values_to = "Value")

ggplot(long_data, aes(x = Success, y = Value, fill = Success)) +
  geom_boxplot() +
  facet_wrap(~Metric, scales='free_y') +
  theme_minimal() +
  labs(title = "Success vs Failure",
       y = "Metric Value",
       x = "Simulation Survival")

#-------------------------------------------------------------------------------

# 5. Feature Engineering

# Create new columns
df_eng <- df %>%
  mutate(
    ParcelsPerMinute = (TotalParcels / Duration) * 60,
    TruckPace = Duration / TotalTrucks,
    AvgTruckSize = TotalParcels / TotalTrucks
  )

# Preview of new columns
df_eng %>%
  select(Success, ParcelsPerMinute, TruckPace, AvgTruckSize) %>%
  head()

#-------------------------------------------------------------------------------

# 6. Forensics

# Filter failures
failures_only <- df_eng %>% filter(Success == F)
failed_metrics <- failures_only %>%
  select(
    Duration,
    StationLoadStdDev,
    MinInterarrival,
    ParcelsPerMinute,
    TruckPace,
    AvgTruckSize
  )

# Correlation Matrix
cor_matrix <- cor(failed_metrics)
failure_factors <- sort(cor_matrix[, "Duration"])

failure_cor_df <- enframe(failure_factors, name = "Metric", value = "Correlation with Duration") %>% filter(Metric != "Duration")
print(failure_cor_df)

# Heatmap
colnames(cor_matrix) <- abbreviate(colnames(cor_matrix), minlength = 4)
rownames(cor_matrix) <- abbreviate(rownames(cor_matrix), minlength = 4)
heatmap(cor_matrix, main = "Correlation Heatmap")

# Scatter Plot: StationLoadStdDev vs Duration
ggplot(failures_only, aes(x = StationLoadStdDev, y = Duration)) +
  geom_point(alpha = 0.5, color = "darkred") +
  geom_smooth(method = "lm", color = "blue") +
  labs(
    title = "The Unfair but Safe Paradox: Imbalance Extends Life",
    subtitle = "Higher StationLoadStdDev (Imabalance) correlates with longer survival",
    x = "Imbalance (StationLoadStdDev)",
    y = "Time to Failure (Duration)"
  ) +
  theme_minimal()

#-------------------------------------------------------------------------------

# 7. Hypothesis Testing

# Theory A
# Calculate Coefficients of Variation before Levene's Tests for result directionality
cv_analysis <- df_eng %>%
  group_by(Success) %>%
  summarise(
    InterarrivalCV = signif(sd(MinInterarrival) / mean(MinInterarrival) * 100, 3)
  )
cat("Instability of Arrival Gaps (Coefficient of Variation):\n")
print(cv_analysis)

# Levene's Test

# Alternative Hypothesis: Failures have higher variance in arrival gaps
levene_arrival <- leveneTest(MinInterarrival ~ Success, data = df_eng)
cat("--- Levene's Test: Arrival Gap Variance ---\n")
print(levene_arrival)

# Robust test for variance homogeneity (Insensitive to normality/outliers)
fligner.test(MinInterarrival ~ Success, data = df_eng)

# Mann-Whitney U/Wilcoxon Test
# Alternative Hypothesis: Failures generally have smaller gaps than Successes
test_interarrival <- wilcox.test(x = df_eng$MinInterarrival[df_eng$Success == T],
                                 y = df_eng$MinInterarrival[df_eng$Success == F],
                                 data = df_eng, 
                                 alternative = "greater")
cat("--- MWU Test: Minimum Interarrival Time ---\n")
print(test_interarrival)

# Theory B
# Alternative Hypothesis: Failures have higher intensity than Successes
test_intensity <- wilcox.test(x = df_eng$ParcelsPerMinute[df_eng$Success == T],
                              y = df_eng$ParcelsPerMinute[df_eng$Success == F],
                              data = df_eng, 
                              alternative = "less")
cat("--- MWU Test: Parcel Intensity ---\n")
print(test_intensity)

#-------------------------------------------------------------------------------

# 8. Logistic Regression

risk_model <- glm(Success ~ ParcelsPerMinute + MinInterarrival + AvgTruckSize, 
                  data = df_eng, 
                  family = "binomial")

# The Raw Coefficients (Log-Odds)
cat("--- Model Summary (Significance Check) ---\n")
summary(risk_model)

# The Odds Ratios
# If number < 1: Reduces odds of success (Bad)
# If number > 1: Increases odds of success (Good)
odds_ratios <- exp(coef(risk_model))

cat("--- Odds Ratios (Risk Multipliers) ---\n")
print(odds_ratios)

# The Safe Operating Envelope

# Create the "Hypothetical" Dataset
simulated_data <- data.frame(
  ParcelsPerMinute = seq(5, 25, by = 0.1),
  MinInterarrival = median(df_eng$MinInterarrival),
  AvgTruckSize = median(df_eng$AvgTruckSize)
)

# Predict the Probability of Success
simulated_data$Prob_Success <- predict(risk_model, 
                                       newdata = simulated_data, 
                                       type = "response")

# Plot the Curve
ggplot(simulated_data, aes(x = ParcelsPerMinute, y = Prob_Success)) +
  geom_line(color = "blue", size = 1.5) +
  
  # Add the "Danger Zone" shading
  geom_area(aes(y = ifelse(Prob_Success < 0.50, Prob_Success, 0)), fill = "red", alpha = 0.3) +
  
  # Add a reference line at 95% Safety (The "Rated Capacity")
  geom_hline(yintercept = 0.95, linetype = "dashed", color = "green") +
  
  # Formatting
  labs(
    title = "System Safety Operating Envelope",
    subtitle = "Probability of Survival vs. Parcel Intensity",
    x = "Intensity (ParcelsPerMinute)",
    y = "Probability of Survival (0-100%)"
  ) +
  theme_minimal()