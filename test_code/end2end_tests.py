import os, subprocess

this_dir = os.fsencode("./")

print("Finding source files to test...")
files_to_test = []
for file in os.listdir(this_dir):
    filename = os.fsdecode(file)
    if filename.endswith(".c"):
        print(f"{filename} found, adding to files to test.")
        files_to_test.append(filename)

print(f"Found {len(files_to_test)} source files. Running tests...")
for file in files_to_test:
    print(f"compiling {file} with clang")
    clang = subprocess.run(executable="clang", args=file, capture_output=True, text=True)
    result = clang.returncode
