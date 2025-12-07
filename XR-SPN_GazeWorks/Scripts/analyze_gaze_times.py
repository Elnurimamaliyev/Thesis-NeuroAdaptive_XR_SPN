import pandas as pd
import matplotlib.pyplot as plt
import numpy as np
import os
import seaborn as sns

def analyze_gaze_times(csv_path):
    # Read the CSV file
    df = pd.read_csv(csv_path, header=0)

    # Debug: Print column names
    print(f"Columns in {csv_path}: {df.columns.tolist()}")

    # Initialize dictionaries to store start and end times
    cross_gazed_starts = {}
    cross_gazed_ends = {}
    icon_gazed_starts = {}
    icon_gazed_ends = {}

    # Initialize lists to store durations
    cross_gazed_durations = []
    icon_gazed_durations = []

    # Process each row in the dataframe
    for _, row in df.iterrows():
        try:
            block = row['Block']
            task = row['Taskcount']
            stage = row['Stage']
            state = row['State']
            time = float(row['Time'])
        except KeyError as e:
            print(f"KeyError: {e}. Please check the column names in the CSV file.")
            return

        # Create a unique key for each event
        key = f"{block}_{task}"

        # Record start and end times for cross_gazed events
        if stage == 'cross_gazed':
            if state == 'start':
                cross_gazed_starts[key] = time
            elif state == 'end' and key in cross_gazed_starts:
                cross_gazed_ends[key] = time
                duration = cross_gazed_ends[key] - cross_gazed_starts[key]
                cross_gazed_durations.append(duration)

        # Record start and end times for icon_gazed events
        if stage == 'icon_gazed':
            if state == 'start':
                icon_gazed_starts[key] = time
            elif state == 'end' and key in icon_gazed_starts:
                icon_gazed_ends[key] = time
                duration = icon_gazed_ends[key] - icon_gazed_starts[key]
                icon_gazed_durations.append(duration)

    # Calculate statistics
    print("\nCross Gazed Statistics:")
    if cross_gazed_durations:
        print(f"Count: {len(cross_gazed_durations)}")
        print(f"Mean Duration: {np.mean(cross_gazed_durations):.4f} seconds")
        print(f"Median Duration: {np.median(cross_gazed_durations):.4f} seconds")
        print(f"Min Duration: {np.min(cross_gazed_durations):.4f} seconds")
        print(f"Max Duration: {np.max(cross_gazed_durations):.4f} seconds")
        print(f"Standard Deviation: {np.std(cross_gazed_durations):.4f} seconds")
    else:
        print("No cross_gazed events found.")

    print("\nIcon Gazed Statistics:")
    if icon_gazed_durations:
        print(f"Count: {len(icon_gazed_durations)}")
        print(f"Mean Duration: {np.mean(icon_gazed_durations):.4f} seconds")
        print(f"Median Duration: {np.median(icon_gazed_durations):.4f} seconds")
        print(f"Min Duration: {np.min(icon_gazed_durations):.4f} seconds")
        print(f"Max Duration: {np.max(icon_gazed_durations):.4f} seconds")
        print(f"Standard Deviation: {np.std(icon_gazed_durations):.4f} seconds")
    else:
        print("No icon_gazed events found.")

    # Create visualizations
    plt.figure(figsize=(12, 6))

    plt.subplot(1, 2, 1)
    sns.histplot(cross_gazed_durations, kde=True)
    plt.title('Cross Gazed Durations')
    plt.xlabel('Duration (seconds)')
    plt.ylabel('Frequency')

    plt.subplot(1, 2, 2)
    sns.histplot(icon_gazed_durations, kde=True)
    plt.title('Icon Gazed Durations')
    plt.xlabel('Duration (seconds)')
    plt.ylabel('Frequency')

    plt.tight_layout()

    # Save the figure
    output_dir = os.path.dirname(csv_path)
    output_path = os.path.join(output_dir, 'gaze_duration_analysis.png')
    plt.savefig(output_path)
    print(f"\nVisualization saved to: {output_path}")

    # Create a dataframe with the results
    results_df = pd.DataFrame({
        'Cross Gazed Durations': pd.Series(cross_gazed_durations),
        'Icon Gazed Durations': pd.Series(icon_gazed_durations)
    })

    # Save the results to a CSV file
    results_path = os.path.join(output_dir, 'gaze_duration_results.csv')
    results_df.to_csv(results_path, index=False)
    print(f"Results saved to: {results_path}")

    # Return the durations for further analysis if needed
    return cross_gazed_durations, icon_gazed_durations

if __name__ == "__main__":
    # Prompt user for the specific file path
    csv_file = input("Enter the full path to the CSV file: ").strip()

    if not os.path.exists(csv_file):
        print(f"File not found: {csv_file}")
    else:
        print(f"\nAnalyzing: {os.path.basename(csv_file)}")
        analyze_gaze_times(csv_file)

    print("\nAnalysis complete!")
