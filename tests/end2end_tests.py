import os, subprocess

code_dir = os.fsencode("./test_code/")

print("Finding source files to test...")
files_to_test = []
for file in os.listdir(code_dir):
    filename = os.fsdecode(file)
    if filename.endswith(".c"):
        print(f"{filename} found, adding to files to test.")
        files_to_test.append(filename)

failed_tests = []
successful_tests = []
print()
print(f"Found {len(files_to_test)} source files. Running tests...")
for file in files_to_test:
    print(f"Compiling {file} with clang")
    exe_name_clang = os.path.splitext(file)[0] + "_clang"
    clang = subprocess.run([f"clang ./test_code/{file} -o {exe_name_clang}"], shell=True, capture_output=True, text=True)
    result = clang.returncode
    if(result != 0):
        print(f"Test failed: failed to compile {file} with clang")
        print()
        failed_tests.append(file)
        continue
    print("Compilation with clang successful")
    print()



    print(f"Compiling {file} with c_compiler")
    exe_name_c_compiler = os.path.splitext(file)[0] + "_c_compiler"
    c_compiler = subprocess.run([f"dotnet run ../tests/test_code/{file} -o ../tests/{exe_name_c_compiler}"], shell=True, cwd="../c_compiler", capture_output=True, text=True)
    result = c_compiler.returncode
    if(result != 0):
        print(f"Test failed: failed to compile {file} with c_compiler")
        print()
        failed_tests.append(file)
        continue
    print("Compilation with c_compiler successful")
    print()

    print(f"Executing {exe_name_clang}")
    exe_clang = subprocess.run(executable=f"./{exe_name_clang}", args="", capture_output=True, text=True)
    print(f"Executing {exe_name_c_compiler}")
    exe_c_compiler = subprocess.run(executable=f"./{exe_name_c_compiler}", args="", capture_output=True, text=True)

    if(exe_clang.returncode != exe_c_compiler.returncode):
        print(f"Test failed: return codes do not match. {file}")
        failed_tests.append(file)
        continue

    if(exe_clang.stdout != exe_c_compiler.stdout):
        print(f"Test failed: outputs do not match. {file}")
        failed_tests.append(file)
        continue

    print(f"Test successful: {file}")
    print()
    successful_tests.append(file)

print(f"{len(successful_tests)} out of {len(files_to_test)} tests succeeded:")
print()
if(len(successful_tests) > 0):
    print("Successful tests:")
    for successful_test in successful_tests:
        print(successful_test)
    print()
if(len(failed_tests) > 0):
    print("Failed tests:")
    for failed_test in failed_tests:
        print(failed_test)
    print()

for file in os.listdir("./"):
    filename = os.fsdecode(file)
    if filename.endswith("clang") or filename.endswith("c_compiler"):
        os.remove(filename)
